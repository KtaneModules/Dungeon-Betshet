using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;
using System;

public class DungeonScript : MonoBehaviour {

	public KMAudio audio;
	public KMBombInfo bomb;

	static int moduleIdCounter = 1;
	int moduleId;
	public bool moduleSolved = false;

	public KMSelectable buttonFwd;
	public KMSelectable buttonR;
	public KMSelectable buttonL;
	public KMSelectable buttonSh;
	public KMSelectable buttonSw;

	public Renderer led;
	public Material ledColor;
	public Material ledUnlit;

	public int level;
	public int levelBuffer = 0;

	public int[] state;
	public int stage;
	public int currentState;

	public int swordHits = 0;
	public int blocks = 0;

	public int didntFight = 0;
	public int nextFight;

	public bool inCombat = false;

	public int sword = 1;
	public int shield = 2;
	public int left = 3;
	public int right = 4;
	public int forward = 5;
	public int qMark = 0;

	public int[][] monsterCodex = { 
		new int[] { 1, 2, 3, 4 },
		new int[] { 5, 6, 7, 8 },
		new int[] { 9, 10, 11, 12 },
		new int[] {13}
	};

	public int currentFight = 0;
	public int currentAction = 0;
	public string monsterName;

	System.Random rand = new System.Random();

	public bool rabbit = false;

	void Awake(){
		moduleId = moduleIdCounter++;
		state = new int[23];
		stage = 1;
		level = 1;


	}

	// Use this for initialization
	void Start () {
		float scalar = transform.lossyScale.x;
		led.transform.GetChild(0).GetComponent<Light>().range *= scalar;

		nextFight = rand.Next (1, 4);
		FindState ();
		Debug.LogFormat ("[Dungeon #{0}] Stage {1}", moduleId, stage);
		Debug.LogFormat ("[Dungeon #{0}] State = {1}", moduleId, currentState);
		Debug.LogFormat ("[Dungeon #{0}] Level = {1}", moduleId, level);


		buttonFwd.OnInteract += delegate () {
			buttonFwd.AddInteractionPunch (1f);
			if(inCombat == false)
				Move(2);
			else
				Action(forward);
			return false;
		};
		buttonL.OnInteract += delegate () {
			buttonL.AddInteractionPunch (1f);
			if(inCombat == false)
				Move(1);
			else
				Action(left);
			return false;
		};
		buttonR.OnInteract += delegate () {
			buttonR.AddInteractionPunch (1f);
			if(inCombat == false)
				Move(3);
			else
				Action(right);
			return false;
		};
		buttonSw.OnInteract += delegate () {
			buttonSw.AddInteractionPunch (1f);
			Action(sword);
			return false;
		};
		buttonSh.OnInteract += delegate () {
			buttonSh.AddInteractionPunch (1f);
			Action(shield);
			return false;
		};
	}

	void IncreaseLevel(){
		if (level < 3) {
			level++;
		}
	}

	void DecreaseLevel(){
		if (level > 1) {
			level--;
		}
	}

	void ChangeState(int newState, int index){
		if (newState > 9) {
			newState = newState % 10;
		}
		state [index] = newState;
		currentState = newState;
	}

	void FindState(){
		if (stage == 1) {
			ChangeState( bomb.GetSerialNumberNumbers ().ElementAt (0), stage-1 );
		} else {
			if (stage < 6) {
				ChangeState( stage + currentState, stage-1 );
			} else {
				if (stage < 11) {
					ChangeState( Math.Abs (currentFight - stage) + Math.Abs (swordHits - currentState), stage -1);
				} else {
					if (stage < 16) {
						if (level == 1) {
							ChangeState (state [stage - 11], stage - 1);
							levelBuffer = 1;
						}
						if (level == 2) {
							ChangeState (state [stage - currentState -2], stage - 1);
							if (currentState < 2)
								levelBuffer = -1;
							if (currentState > 7)
								levelBuffer = 1;
						}
						if (level == 3) {
							ChangeState (state [stage - 6] + blocks, stage - 1);
							levelBuffer = -1;
						}
					} else {
						ChangeState (Math.Min (currentState, swordHits) + Math.Max (currentFight, blocks), stage - 1);
						if (didntFight >= 2)
							levelBuffer = 1;
					}
				}
			}
		}
	}

	void ChangeLevel(){
		if (stage == 6)
			IncreaseLevel ();
		if (stage == 11)
			IncreaseLevel ();
		if (stage == 16)
			level = 3;	
		if (levelBuffer == 1)
			IncreaseLevel ();
		if (levelBuffer == -1)
			DecreaseLevel ();
		levelBuffer = 0;
	}

	void Move(int direction){
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (!moduleSolved) {
			if (didntFight == nextFight) {
				if (levelBuffer == 1)
					IncreaseLevel ();
				if (levelBuffer == -1)
					DecreaseLevel ();
				levelBuffer = 0;
				Combat ();
				didntFight = 0;
				nextFight = rand.Next (1, 4);
			} else {
				didntFight++;
				if (currentState < 3) {
					if (direction == 1) {
						stage++;
						ChangeLevel ();
						FindState ();
					} else {
						GetComponent<KMBombModule> ().HandleStrike ();
						Debug.LogFormat ("[Dungeon #{0}] Wrong move : the state is {1}, the correct direction was left.", moduleId, currentState);
					}
				} else {
					if (currentState >= 3 && currentState < 7) {
						if (direction == 2) {
							stage++;
							ChangeLevel ();
							FindState ();
						} else {
							GetComponent<KMBombModule> ().HandleStrike ();
							Debug.LogFormat ("[Dungeon #{0}] Wrong move : the state is {1}, the correct direction was forward.", moduleId, currentState);
						}
					} else {
						if (currentState >= 7) {
							if (direction == 3) {
								stage++;
								ChangeLevel ();
								FindState ();
							} else {
								GetComponent<KMBombModule> ().HandleStrike ();
								Debug.LogFormat ("[Dungeon #{0}] Wrong move : the state is {1}, the correct direction was right.", moduleId, currentState);
							}
						}
					}
				}

				if (stage == 23) {
					moduleSolved = true;
					GetComponent<KMBombModule> ().HandlePass ();
				} else {
					Debug.LogFormat ("[Dungeon #{0}] Stage {1}", moduleId, stage);
					Debug.LogFormat ("[Dungeon #{0}] State = {1}", moduleId, currentState);
					Debug.LogFormat ("[Dungeon #{0}] Level = {1}", moduleId, level);
				}
			}
		}


	}

	void Action(int action){
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (!moduleSolved) {
			if (inCombat == false) {
				GetComponent<KMBombModule> ().HandleStrike ();
				Debug.LogFormat ("[Dungeon #{0}] Wrong action : there is no combat.", moduleId);
			}
			else {
				if (Codex (currentFight) [currentAction] != action) {
					GetComponent<KMBombModule> ().HandleStrike ();
					if (Codex (currentFight) [currentAction] == shield) {
						Debug.LogFormat ("[Dungeon #{0}] Wrong action : the action #{1} against that monster was block.", moduleId, currentAction);
					}
					if (Codex (currentFight) [currentAction] == sword) {
						Debug.LogFormat ("[Dungeon #{0}] Wrong action : the action #{1} against that monster was sword.", moduleId, currentAction);
					}
					if (Codex (currentFight) [currentAction] == forward) {
						Debug.LogFormat ("[Dungeon #{0}] Wrong action : the action #{1} against that monster was forward.", moduleId, currentAction);
					}
					if (Codex (currentFight) [currentAction] == left) {
						Debug.LogFormat ("[Dungeon #{0}] Wrong action : the action #{1} against that monster was left.", moduleId, currentAction);
					}
					if (Codex (currentFight) [currentAction] == right) {
						Debug.LogFormat ("[Dungeon #{0}] Wrong action : the action #{1} against that monster was right.", moduleId, currentAction);
					}
				} else {
					if (action == sword)
						swordHits++;
					if (action == shield)
						blocks++;
					if (currentAction == Codex (currentFight).Length - 1) {
						EndCombat ();
					} else
						currentAction++;
				}
			}
		}
	}

	void Combat(){
		inCombat = true;
		bool found = false;
		led.material = ledColor;
		led.transform.GetChild(0).GetComponent<Light>().color = Color.white;
		for (int i = 0; i < 4; i++) {
			if (monsterCodex [level - 1] [i] != 0) {
				currentFight = monsterCodex [level - 1] [i];
				monsterCodex [level - 1] [i] = 0;
				found = true;
				break;
			}
		}
		if (found == false) {
			if (level < 3) {
				IncreaseLevel ();
				Combat ();
				DecreaseLevel ();				
			} else {
				currentFight = monsterCodex [3] [0];
			}
		}
		Codex (currentFight);
		Debug.LogFormat ("[Dungeon #{0}] Fighting {1}", moduleId,monsterName);
	}

	void EndCombat(){
		inCombat = false;

		currentAction = 0;
		led.material = ledUnlit;
		led.transform.GetChild(0).GetComponent<Light>().color = Color.black;

		if (stage > 15)
			DecreaseLevel ();
		stage++;
		ChangeLevel ();
		FindState ();
		Debug.LogFormat ("[Dungeon #{0}] End of fight : Sword hits = {1}, Blocks = {2}, Last monster fought = {3}", moduleId, swordHits,blocks,currentFight);
		Debug.LogFormat ("[Dungeon #{0}] Stage {1}", moduleId, stage);
		Debug.LogFormat ("[Dungeon #{0}] State = {1}", moduleId, currentState);
		Debug.LogFormat ("[Dungeon #{0}] Level = {1}", moduleId, level);
	}

	int[] Codex(int nb){
		
		switch (nb) {
		case 1:
			monsterName = "Bat";
			qMark = sword;
			return new int[] { sword };
		case 2:
			monsterName = "Snake";
			qMark = shield;
			return new int[] { shield, sword };

		case 3:
			monsterName = "Ghost";
			qMark = shield;
			return new int[] { shield, shield };

		case 4:
			monsterName = "Zombie";
			qMark = sword;
			return new int[] { sword, sword };

		case 5:
			monsterName = "Cyclops";
			qMark = shield;
			return new int[] { sword,shield ,sword };

		case 6:
			monsterName = "Golem";
			qMark = sword;
			return new int[] { shield,shield ,sword ,sword};

		case 7:
			monsterName = "Ghoul";
			qMark = sword;
			return new int[] { sword,forward ,shield ,forward,sword,sword};

		case 8:
			monsterName = "Lich";
			qMark = shield;
			return new int[] { sword,left ,shield ,right,shield,sword};

		case 9:
			monsterName = "Dragon";
			qMark = shield;
			return new int[] { shield,shield ,shield ,sword,shield,shield,sword,shield,shield,sword};

		case 10:
			monsterName = "Demon";
			qMark = right;
			return new int[] { sword,right ,left ,left,right,shield,sword,forward,right,sword };

		case 11:
			monsterName = "Rabbit";
			foreach (string str in bomb.GetIndicators()) {
				if (str.Equals ("BOB"))
					rabbit = true;
			}
			if (rabbit) {
				qMark = right;
				return new int[] { shield, shield, shield, shield, shield, left, shield, right, shield };
			} else {
				qMark = sword;
				return new int[] { shield, sword, sword };
			}

		case 12:
			monsterName = "Vampire";
			qMark = left;
			return new int[] { sword,sword ,forward ,shield,sword,right,shield,sword,shield,left, sword };
		case 13:
			monsterName = "Last monster";
			int qBuff = qMark;
			qMark = sword;
			return new int[] { sword,shield ,shield ,shield,left,forward,qBuff,shield,right,forward, shield,sword,sword, forward,sword,left,shield,sword,sword };
		}
		return null;

	}
}
