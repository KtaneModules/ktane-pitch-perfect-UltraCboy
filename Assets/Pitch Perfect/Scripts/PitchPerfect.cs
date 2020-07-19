using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchPerfect : MonoBehaviour {
	public KMSelectable PlayButton, SubmitButton, CycleFlat, CycleSharp, ReferencePitch;
	public KMAudio PPKMAudio;
	public KMBombModule PPModule;
	public AudioSource PPAudio, SolveAudio;
	public TextMesh MainText;
	public MeshRenderer[] StageInd;
	public Color On, Off, Red;
	public FakeStatusLight Light;
	public Material RAMat, RUMat, StageOn, StageOff; // RA/RU - Ref (Un)Available
	public MeshRenderer RefInd;
	public TextMesh[] StrikeText; //Length indicates fake strikes before real strike
	public static int moduleID = 0;
	public int thisModuleID;

	private readonly string[] NoteNames =
	{
		"C", "C♯/D♭", "D", "D♯/E♭", "E", "F", "F♯/G♭", "G", "G♯/A♭", "A", "A♯/B♭", "B"
	};
	private bool[] RedNotes;
	private int CurrentNote, NoteIndex, Answer, CurrentStage, NumStrikes;
	private bool RefAvailable, IsSolved;

	// Use this for initialization
	void Start () {
		moduleID++; thisModuleID = moduleID;
		CurrentNote = GenerateNote(); Answer = 0;
		CurrentStage = 0;
		RefAvailable = true;
		NumStrikes = 0;
		RedNotes = new bool[NoteNames.Length];
		for (int i = 0; i < RedNotes.Length; i++) RedNotes[i] = false;
		IsSolved = false;

		PlayButton.OnInteract += delegate { HandlePress(0); return false; };
		ReferencePitch.OnInteract += delegate { HandlePress(1); return false; };
		CycleFlat.OnInteract += delegate { HandlePress(2); return false; };
		CycleSharp.OnInteract += delegate { HandlePress(3); return false; };
		SubmitButton.OnInteract += delegate { HandlePress(4); return false; };

		PPModule.OnPass += delegate { 
			SolveAudio.Play();
			IsSolved = true;
			return true; 
		};

		Light = Instantiate(Light);
		Light.GetStatusLights(PPModule.GetComponent<Transform>());
		Light.Module = PPModule;
	}
	// Update is called once per frame
	void Update () {
		//Empty, I'm sure I'll need this for something but not at the moment
	}
	public void HandlePress(int ButtonID)
	{
		PPKMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, PPModule.GetComponent<Transform>());
		switch (ButtonID)
		{
			case 0: PlayNote(CurrentNote); break;
			case 1:
				if (RefAvailable)
				{
					PlayNote(Answer); RefAvailable = false;
					RefInd.material = RUMat;
				}
				break;
			case 2: CycleMainScreen(-1); break;
			case 3: CycleMainScreen(1); break;
			case 4: 
				Submit();
				GetComponent<KMSelectable>().AddInteractionPunch();
				break;
		}
	}
	void DebugMsg(string msg)
	{
		Debug.LogFormat("[Pitch Perfect #{0}] {1}", thisModuleID, msg);
	}
	private int GenerateNote()
	{
		int min = 0, max = 12;
		for(int i = 0; i < CurrentStage; i++)
		{
			min -= 12; max += 12;
		}
		int output = Random.Range(min, max);
		NoteIndex = output;
		while (NoteIndex < 0) NoteIndex += 12;
		while (NoteIndex >= 12) NoteIndex -= 12;
		DebugMsg("The generated note for Stage " + (CurrentStage + 1) + " is " + NoteNames[NoteIndex]);
		return output;
	}
	public void CycleMainScreen(int dir)
	{
		Answer += dir;
		while (Answer >= 12) Answer -= 12;
		while (Answer < 0) Answer += 12;
		MainText.text = NoteNames[Answer];
		if (RedNotes[Answer]) MainText.color = Red;
		else MainText.color = On;
	}
	public void PlayNote(int note)
	{
		float transpose = -12; // -12 -> 1 octave lower
		PPAudio.pitch = Mathf.Pow(2, (note + transpose) / 12f);
		PPAudio.Play();
	}
	public void Submit()
	{
		if (IsSolved) return;
		DebugMsg(NoteNames[Answer] + " was submitted");
		if (Answer == NoteIndex) //If Correct
		{
			DebugMsg("Answer correct");
			CurrentStage++;
			for (int i = 0; i < RedNotes.Length; i++) RedNotes[i] = false;
			if (CurrentStage >= 3)
			{
				Light.HandlePass();
				DebugMsg("Three stages have been passed. Module solved.");
			}
			else CurrentNote = GenerateNote();
		}
		else //If Incorrect
		{
			NumStrikes++;
			if (NumStrikes < StrikeText.Length + 1)
			{
				Light.FlashStrike();
				PPKMAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.Strike, PPModule.GetComponent<Transform>());
				RedNotes[Answer] = true;
				MainText.color = Red;
				DebugMsg("Answer incorrect. Fake strike recorded.");
			}
			else
			{
				Light.HandleStrike();
				RefAvailable = true;
				RefInd.material = RAMat;
				CurrentNote = GenerateNote();
				CurrentStage = 0;
				NumStrikes = 0;
				for (int i = 0; i < RedNotes.Length; i++) RedNotes[i] = false;
				MainText.color = On;
				DebugMsg("Answer incorrect. Real strike recorded.");
			}
			for(int i = 0; i < StrikeText.Length; i++)
			{
				TextMesh t = StrikeText[i];
				if (NumStrikes > i) t.color = On;
				else t.color = Off;
			}
		}
		//Regardless
		for (int i = 0; i < StageInd.Length; i++)
		{
			if (i < CurrentStage) StageInd[i].material = StageOn;
			else StageInd[i].material = StageOff;
		}
	}
}
