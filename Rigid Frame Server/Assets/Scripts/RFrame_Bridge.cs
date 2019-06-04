using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace RigidFrame_Development
{
	
	public class RFrame_Bridge : MonoBehaviour {

		RFrame_Sim frameSimulator = new	RFrame_Sim();

		public string LinkName;
		public int affirmCycles = 300;

		public bool fakeGravityToggle = true;
		public bool displayStrutsOnLoad = false;
		public bool displayComponentRepsOnLoad = true;

		public Text textBoxForCyclesUpdate;
		public InputField inputFieldForNumCycles;
		public InputField maxSpeedOnBalPols;
		public InputField maxLegLiftBalPols;
		float lastUpdate = 0.0f;
		// Use this for initialization
		void Start () {
			//frameSimulator = new RFrame_Sim();
		}
		
		// Update is called once per frame
		void Update () {
			// Check if frame is ready to be loaded

			// Run the cycles on each frame.
			float timeBegin = Time.realtimeSinceStartup;
			frameSimulator.performCyclesV2(GetComponentsInChildren<RFrame_Object>(), affirmCycles);
			float cyclesDuration = Time.realtimeSinceStartup - timeBegin;
			if(textBoxForCyclesUpdate!=null){
				float timeCatch = Time.realtimeSinceStartup;
				if(timeCatch > lastUpdate + 1.0f) {
					// Update the text box only when half a second has passed.
					float affirmFloat = affirmCycles;
					float durationCalc = 1.0f / (cyclesDuration / affirmFloat);
					string message = affirmCycles + " cycles for " + durationCalc + " CPS";
					textBoxForCyclesUpdate.text = message;
					lastUpdate = timeCatch;
				}
			}
		}
		
		public void toggleFakeGravity(bool setting) {
			fakeGravityToggle = setting;
		}

		public void setNumberOfCycles() {
			affirmCycles = int.Parse(inputFieldForNumCycles.text);
		}

		public void setMaxSpeedOnBalPols() {
			foreach(RFrame_Object frameObject in GetComponentsInChildren<RFrame_Object>()) {
				BMod_BalancePosition balPol = frameObject.GetComponentInChildren<BMod_BalancePosition>();
				balPol.maxSlideMovementSpeed = float.Parse(maxSpeedOnBalPols.text);
			}
		}

		public void setMaxLegLiftOnBalPols() {
			foreach(RFrame_Object frameObject in GetComponentsInChildren<RFrame_Object>()) {
				BMod_BalancePosition balPol = frameObject.GetComponentInChildren<BMod_BalancePosition>();
				balPol.maxReleasable = int.Parse(maxLegLiftBalPols.text);
			}
		}
	}

		// The main class that holds the simulation
	public class RFrame_Sim {
			
		bool workAlignSwitch = false;
		//float adjustmentThreshhold = 0; //0.001f;
		float affirmRatio = 0.01f;
		//int affirmPerCycleIn = 100;
		
		// TODO - Plan in distance groups. Where only when distance groups fall
		//        within range do they test against each others vertices.
		
		//Frame_Object frameObject = null; // The frame being held together
		
		/**
		* Basic constructor
		*/
		public RFrame_Sim() {
		}
		
		// public void setFrameObject(Frame_Object givenFrameObject) {
		// 	frameObject = givenFrameObject;
		// 	adjustmentThreshhold = frameObject.shortestStrutLength * 0.05f;
		// 	affirmRatio = adjustmentThreshhold * 0.1f;
		// }

		/**
		* This should be the main calling method for performing the affirmation
		* cycles on the attached Frame_Object.
		* This method uses a work alignment boolean flag as a way of causing
		* vertices to automatically ready themselves when a translation is made
		* to one of them. If their work alignment flag does not match the cycles
		* work alignment flag, then it resets its values and starts again.
		* The plan is to reduce the amount of full calls to the allVertices array
		* in the frame object. (Although for now, I only see one reduction. But if
		* there's 100 cycles happening, that's 100 less loops to do per cycle.)
		* @param affirmPerCycle How many cycles to perform.
		*/
		public void performCycles(RFrame_Object[] frameObjects, int affirmPerCycle) {
			// ******************************************************************
			// ******************************************************************
			// ******************************************************************
			workAlignSwitch = !workAlignSwitch; // Swap over the work alignment
			RFrame_Strut strut;
			if (frameObjects!=null) {
				if(frameObjects.Length==0) { return; } // Bin out if there's nothing to work on.
				//Debug.Log("Frame objects detected. Peforming cycles");
				for (int i=0; i<affirmPerCycle; i++) {
					// Pass through each frame in the array and process their vertices and struts.
					foreach(RFrame_Object frameObject in frameObjects) {
						// Set the adjustment values per frame.
						//adjustmentThreshhold = 0.0f; //frameObject.GetShortestStrutLength() * 0.05f;
						affirmRatio = 1.0f;//adjustmentThreshhold * 0.2f;
						// Work through strut array and affirm strut lengths
						foreach (GameObject strutGameObject in frameObject.struts) {
							strut = strutGameObject.GetComponent<RFrame_Strut>();
							// Collect the vertices involved with this strut.
							RFrame_Vertex v1 = frameObject.allVertices[strut.v1].GetComponent<RFrame_Vertex>();
							RFrame_Vertex v2 = frameObject.allVertices[strut.v2].GetComponent<RFrame_Vertex>();
							// Get the distance between the two, along with normalised
							// axis vector pointing in the direction of the two.
							float[] distNormV1 = v1.distanceToVertex(v2,true);
							strut.actualLength = distNormV1[0]; // Record the current length to the strut.
							// No need to call the method for the other way round.
							// Just reverse the normals.

							float limitDiff = strut.distanceOutFromAcceptableRange(distNormV1[0]);// strut.length - distNormV1[0];
							// If the difference between the length and measured distance
							// between the two vertices, is greater than the adjustmentThreshold
							// variable, then adjust the vertices to the correct distance
							// IDEA - Have a think about percentage shift bias for each vertex, depending on how many struts are connected to it.
							//if (Mathf.Abs(limitDiff)>adjustmentThreshhold) {
								// Calculate the translation for each vertex.
							float v1xShift = -(limitDiff*affirmRatio*distNormV1[1]);
							float v1yShift = -(limitDiff*affirmRatio*distNormV1[2]);
							float v1zShift = -(limitDiff*affirmRatio*distNormV1[3]);
							// This will simply be the reverse direction.
							float v2xShift = -v1xShift;
							float v2yShift = -v1yShift;
							float v2zShift = -v1zShift;
							// Applying the translations to the vertices.
							// Only apply if not locked in place.
							if (!v1.lockedInPlace) {
								v1.applyTranslation(v1xShift, v1yShift, v1zShift, this);
							} else {
								v1.applyTranslation(0, 0, 0, this);
							}
							if (!v2.lockedInPlace) {
								v2.applyTranslation(v2xShift, v2yShift, v2zShift, this);
							} else {
								v2.applyTranslation(0, 0, 0, this);
							}
							//}
						}
						// For each vertex that was effected, have it's movement divided
						// by the amount of effects applied to it.
						foreach (GameObject vertex in frameObject.allVertices) {
							vertex.GetComponent<RFrame_Vertex>().finaliseTranslations(this);
						}
						// Updating the line renderers
						// RFrame_Strut strutPull;
						// foreach (GameObject strutObj in frameObject.struts) {
						// 	strutPull = strutObj.GetComponent<RFrame_Strut>();
						// 	GameObject v1Obj = frameObject.allVertices[strutPull.v1];
						// 	GameObject v2Obj = frameObject.allVertices[strutPull.v2];
						// 	Vector3[] positions = {v1Obj.transform.position,v2Obj.transform.position};
						// 	strutPull.updateLineRenderPositions(positions);
						// }
					}
				}
			}
			
			// ******************************************************************
			// ******************************************************************
			// ******************************************************************
		}
		
		public void performCyclesV2(RFrame_Object[] frameObjects, int affirmPerCycle) {
			// ******************************************************************
			// ******************************************************************
			// ******************************************************************
			RFrame_Strut strut;
			if (frameObjects!=null) {
				if(frameObjects.Length==0) { return; } // Bin out if there's nothing to work on.
				//Debug.Log("Frame objects detected. Peforming cycles");
				for (int i=0; i<affirmPerCycle; i++) {
					// Update the work align switch - Moved to here from the start of the function
					// on the 20 March 2019.
					workAlignSwitch = !workAlignSwitch; // Swap over the work alignment

					// Pass through each frame in the array and process their vertices and struts.
					foreach(RFrame_Object frameObject in frameObjects) {
						// Set the adjustment values per frame.
						//adjustmentThreshhold = 0.0f; //frameObject.GetShortestStrutLength() * 0.05f;
						affirmRatio = 1.0f;//adjustmentThreshhold * 0.2f;
						// Work through strut array and affirm strut lengths
						foreach (GameObject strutGameObject in frameObject.struts) {
							strut = strutGameObject.GetComponent<RFrame_Strut>();
							// Collect the vertices involved with this strut.
							RFrame_Vertex v1 = frameObject.allVertices[strut.v1].GetComponent<RFrame_Vertex>();
							RFrame_Vertex v2 = frameObject.allVertices[strut.v2].GetComponent<RFrame_Vertex>();
							// Get the distance between the two, along with normalised
							// axis vector pointing in the direction of the two.
							float[] distNormV1 = v1.distanceToVertex(v2,true);
							strut.actualLength = distNormV1[0]; // Record the current length to the strut.
							// No need to call the method for the other way round.
							// Just reverse the normals.

							float limitDiff = strut.distanceOutFromAcceptableRange(distNormV1[0]);// strut.length - distNormV1[0];
							// If the difference between the length and measured distance
							// between the two vertices, is greater than the adjustmentThreshold
							// variable, then adjust the vertices to the correct distance
							// IDEA - Have a think about percentage shift bias for each vertex, depending on how many struts are connected to it.
							//if (Mathf.Abs(limitDiff)>adjustmentThreshhold) {
								// Calculate the translation for each vertex.
							float v1xShift = -(limitDiff*affirmRatio*distNormV1[1]);
							float v1yShift = -(limitDiff*affirmRatio*distNormV1[2]);
							float v1zShift = -(limitDiff*affirmRatio*distNormV1[3]);
							// This will simply be the reverse direction.
							float v2xShift = -v1xShift;
							float v2yShift = -v1yShift;
							float v2zShift = -v1zShift;
							// Applying the translations to the vertices.
							// Only apply if not locked in place.
							if (!v1.lockedInPlace) {
								v1.applyTranslationV2(v1xShift, v1yShift, v1zShift, this);
							} else {
								v1.applyTranslationV2(0, 0, 0, this);
							}
							if (!v2.lockedInPlace) {
								v2.applyTranslationV2(v2xShift, v2yShift, v2zShift, this);
							} else {
								v2.applyTranslationV2(0, 0, 0, this);
							}
							//}
						}
						// For each vertex that was effected, have it's movement divided
						// by the amount of effects applied to it.
						// foreach (GameObject vertex in frameObject.allVertices) {
						// 	vertex.GetComponent<RFrame_Vertex>().finaliseTranslations(this);
						// }
						// Updating the line renderers
						// RFrame_Strut strutPull;
						// foreach (GameObject strutObj in frameObject.struts) {
						// 	strutPull = strutObj.GetComponent<RFrame_Strut>();
						// 	GameObject v1Obj = frameObject.allVertices[strutPull.v1];
						// 	GameObject v2Obj = frameObject.allVertices[strutPull.v2];
						// 	Vector3[] positions = {v1Obj.transform.position,v2Obj.transform.position};
						// 	strutPull.updateLineRenderPositions(positions);
						// }
					}
				}
			}
			
			// ******************************************************************
			// ******************************************************************
			// ******************************************************************
		}
		
		public bool getWorkAlignment() {
			return workAlignSwitch;
		}

	}

}