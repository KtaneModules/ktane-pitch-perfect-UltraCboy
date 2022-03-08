using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PitchPerfect : MonoBehaviour {

	public KMSelectable PlayButton, SubmitButton, CycleFlat, CycleSharp, ReferencePitch;
	public KMAudio PPKMAudio;
	public KMBombModule PPModule;
	public AudioSource PPAudio, LowCAudio, SolveAudio;
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

		Light = Instantiate(Light);
		Light.GetStatusLights(PPModule.GetComponent<Transform>());
		Light.Module = PPModule;

		PPModule.OnPass += delegate {
			SolveAudio.Play();
			IsSolved = true;
			return true;
		};
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
		DebugMsg("The generated note for Stage " + (CurrentStage + 1) + " is " + NoteNames[NoteIndex] + "(Value: " + output + ")");
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
		AudioSource Sound; float transpose;
		if (note > -8)
		{
			transpose = -12;
			Sound = PPAudio;
		}
		else
		{
			transpose = 12;
			Sound = LowCAudio;
		} //LowC < (note = 0) < PPAudio
		Sound.pitch = Mathf.Pow(2, (note + transpose) / 12f);
		Sound.Play();
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
#pragma warning disable 414
	private string TwitchHelpMessage = "!{0} [note name] to cycle screen to that note. Use # for sharp and b for flat. " +
		"Either flat, sharp, or both will work (ie c#, db, and c#/db will work) Type !{0} play to play stage note, " +
		"!{0} ref to play reference, !{0} submit to submit. Next stage note will automatically play on a correct answer.";
#pragma warning restore 414
	private string[,] AcceptableNames =
	{
		{ "c", null, null },
		{ "c#", "db", "c#/db" },
		{ "d", null, null },
		{ "d#", "eb", "d#/eb" },
		{ "e", null, null },
		{ "f", null, null },
		{ "f#", "gb", "f#/gb" },
		{ "g", null, null },
		{ "g#", "ab", "g#/ab" },
		{ "a", null, null },
		{ "a#", "bb", "a#/bb" },
		{ "b", null, null }
	};
	private KMSelectable[] ProcessTwitchCommand(string command)
	{
		command = command.ToLowerInvariant().Trim();
		switch (command)
		{
			case "play": return new KMSelectable[] { PlayButton };
			case "submit":
				if (Answer == NoteIndex) return new KMSelectable[] { SubmitButton, PlayButton };
				else return new KMSelectable[] { SubmitButton };
			case "ref": return new KMSelectable[] { ReferencePitch };
			default: //Cycling the main screen
				List<KMSelectable> buttonList = new List<KMSelectable>();
				int valueToGoTo = -1;
				for (int i = 0; i < 12; i++){
					bool found = false;
					for(int j = 0; j < 3; j++)
					{
						if (AcceptableNames[i, j] == null) break;
						if (AcceptableNames[i, j].Equals(command))
						{
							valueToGoTo = i; found = true; break;
						}
					}
					if (found) break;
				}
				if (valueToGoTo == -1) return null;
				int dif = valueToGoTo - Answer;
				while (dif < 0) dif += 12;
				for (int i = 0; i < dif; i++) buttonList.Add(CycleSharp);
				return buttonList.ToArray();
		}
	}
	IEnumerator TwitchHandleForcedSolve()
    {
		// This function may be re-written at a later date to do an actual autosolve, but this will suffice for now.
		Light.HandlePass();
		DebugMsg("Module force-solved.");
		yield return new WaitForSeconds(0f);
    }
}
