using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Version 2.0 Andrew Forster
// 12 March 2019
// Added support to the angle linker for detecting line based measuring (dot product)

namespace RigidFrame_Development
{
	public class RFrame_Object : MonoBehaviour {

		string frameName;
		float shortestStrutLength = -1f;
		
		public List<GameObject> allVertices; // Array of all Fvertex objects. Each Fvertex object has an array of Fstruts
		public List<GameObject> struts; // Array of Fstrut objects.
		public List<GameObject> angleLinks; // Array of angle measurer links.
		public List<GameObject> compReps; // Array of component representors.
		public TextAsset objFileToBuildFrom = null;

		public GameObject VertexInstantiator;
		public GameObject StrutInstantiator;
		public GameObject AngleViewerInstantiator;
		public GameObject ComponentRepresentorInstantiator;

		public GameObject BalancePositionInstantiator;
		public GameObject DistanceHolderInstantiator;
		public GameObject AnchorPointInstantiator;

		// Use this for initialization. If a OBJ file has been attached then call
		// the build function to turn it into a working frame.
		void Start () {
			if (objFileToBuildFrom!=null) {
				createFromOBJ(objFileToBuildFrom);
			}
		}
		
		// Update is called once per frame
		void Update () {
			
		}

		/**
			* Test method for creating a Frame_Object from an OBJ file
			* 23 Jan 2019 - Updated to read from modified OBJ files.
			* @param objFile The filename of the OBJ file
			* @return A Frame_Object file
			* @throws FileNotFoundException 
			*/
		void createFromOBJ(TextAsset objFile) {
			//System.out.println("Loading OBJ file : " + objFile.toString());

			string[] inFile = objFile.text.Split('\n');
			
			List<GameObject> verticesBuild = new List<GameObject>();
			List<GameObject> strutBuild = new List<GameObject>();
			List<GameObject> angleViewBuild = new List<GameObject>();
			List<GameObject> compRepBuild = new List<GameObject>();
			List<GameObject> balPosBuild = new List<GameObject>();
			List<GameObject> disHolBuild = new List<GameObject>();
			List<GameObject> ancPoiBuild = new List<GameObject>();

			int vertexIndex = 0;

			foreach(string objLine in inFile) {
				//System.out.println(objLine); // Debug print to console the current line.
				// Reading in a vertex line
				if (objLine.StartsWith("v") && !objLine.StartsWith("vn") && !objLine.StartsWith("vt") && !objLine.StartsWith("vp")) {
					// Split the string using space. e.g. v 3.00 2.00 -2.00
					//System.out.println("Vertex : " + objLine);
					string[] splits = objLine.Split(' ');
					float x = float.Parse(splits[1]);
					float y = float.Parse(splits[2]);
					float z = float.Parse(splits[3]);
					
					GameObject vertexGameObject = Instantiate(VertexInstantiator, this.transform);
					vertexGameObject.name = vertexGameObject.name + "_" + vertexIndex;
					vertexGameObject.transform.localPosition = new Vector3(x,y,z);
					RFrame_Vertex vertexScript = vertexGameObject.GetComponent<RFrame_Vertex>();
					vertexScript.x = x;
					vertexScript.y = y;
					vertexScript.z = z;
					// Check for lock character
					if (splits.Length>4) {
						if (splits[4]=="l") {
							vertexScript.lockedInPlace = true;
						}
					}
					verticesBuild.Add(vertexGameObject);
					vertexIndex++;
					
				} else if (objLine.StartsWith("l")) {
					// This is for any standalone lines, which will be turned into struts.
					// Modified to only work with the first two vertice indexes
					
					string[] splits = objLine.Split(' ');
					// Remember that indexes start at 1 in the obj/txt file and need converting to zero-indexed.
					int v1 = int.Parse(splits[1]) - 1;
					int v2 = int.Parse(splits[2]) - 1;
					GameObject strutGameObject = RFrame_Object.createStrutIfNotExisting(this, StrutInstantiator, strutBuild, v1, v2);
					// Check if strut is displayed. This is determined by having more than three entries in splits.
					// Basically any character entered after the 2 indexes will trigger this.
					//if(strutGameObject!=null && splits.Length > 3) {
					if (splits.Length==5) {
						RFrame_Strut strutScript = strutGameObject.GetComponent<RFrame_Strut>();
						strutScript.minLength = float.Parse(splits[3]);
						strutScript.maxLength = float.Parse(splits[4]);
					}
					// All line renderers will be switched off.
					LineRenderer lineRenderer = strutGameObject.GetComponent<LineRenderer>();
					// Used when debugging the a fresh OBJ frame.
					lineRenderer.enabled = transform.GetComponentInParent<RFrame_Bridge>().displayStrutsOnLoad;
					//}
				} else if (objLine.StartsWith("anglink")) {
					// Angle link entry.
					// Requires 4 vertex indexes and a link channel number which will represent the angle measurer struts.
					// and the actuator/motor/servo this is linked too.
					// Centering and angle limits should be entered.
					string[] splits = objLine.Split(' ');
					int centerVertex = int.Parse(splits[1]);
					int upVertex = int.Parse(splits[2]);
					int measure1 = int.Parse(splits[3]);
					int measure2 = int.Parse(splits[4]);
					int channel = int.Parse(splits[5]); // Channel number should already be zero-indexed.
					bool invertChannel = bool.Parse(splits[6]); // Should be 0, 1, true or false
					float minChannelAngle = float.Parse(splits[7]); // Channel won't be sent any number smaller than this.
					float maxChannelAngle = float.Parse(splits[8]); // Channel won't be sent any number larger than this.
					float biasAngle = float.Parse(splits[9]); // The bias to apply. Indicates that a servo may need recentering
					int angleMeasureType = int.Parse(splits[10]); // Quaternion or dot product angle
					float scaleChangedAngled = float.Parse(splits[11]); // How much to scale the change in angle from initial. Increases range of movement.
					GameObject angleViewerObj = Instantiate(AngleViewerInstantiator, this.transform);
					RFrame_AngleLink angleLinker = angleViewerObj.GetComponent<RFrame_AngleLink>();
					angleLinker.centerVertex = centerVertex;
					angleLinker.upVertex = upVertex;
					angleLinker.measure1 = measure1;
					angleLinker.measure2 = measure2;
					angleLinker.linkChannel = channel;
					angleLinker.invertedSendAngle = invertChannel;
					angleLinker.minimumAngle = minChannelAngle;
					angleLinker.maximumAngle = maxChannelAngle;
					angleLinker.biasAngle = biasAngle;
					angleLinker.angleChangeScaling = scaleChangedAngled;
					switch (angleMeasureType) {
						case 0: {
							angleLinker.angleMeasureType = RFrame_AngleLink.AngleMeasureTypeEnum.quaternionBased;
							break;
						}
						case 1: {
							angleLinker.angleMeasureType = RFrame_AngleLink.AngleMeasureTypeEnum.lineBased;
							break;
						}
						default: {
							angleLinker.angleMeasureType = RFrame_AngleLink.AngleMeasureTypeEnum.quaternionBased;
							break;
						}
					}
					angleViewBuild.Add(angleViewerObj);
				} else if (objLine.StartsWith("comprep") && transform.GetComponentInParent<RFrame_Bridge>().displayComponentRepsOnLoad) {
					// Component representation
					string[] splits = objLine.Split(' ');
					int centerVertex = int.Parse(splits[1]);
					int upVertex = int.Parse(splits[2]);
					int forwardVertex = int.Parse(splits[3]);
					string meshName = splits[4];
					float xRot = float.Parse(splits[5]);
					float yRot = float.Parse(splits[6]);
					float zRot = float.Parse(splits[7]);
					float xTran = float.Parse(splits[8]);
					float yTran = float.Parse(splits[9]);
					float zTran = float.Parse(splits[10]);
					GameObject compRepObj = Instantiate(ComponentRepresentorInstantiator, this.transform);
					RFrame_Component componentRep = compRepObj.GetComponent<RFrame_Component>();
					componentRep.centerVertex = centerVertex;
					componentRep.forwardVertex = forwardVertex;
					componentRep.upVertex = upVertex;
					GameObject meshObject = Instantiate(Resources.Load<GameObject>(meshName), compRepObj.transform);
					meshObject.transform.localPosition = new Vector3(xTran,yTran,zTran);
					meshObject.transform.localRotation = Quaternion.Euler(xRot, yRot, zRot);
				} else if (objLine.StartsWith("balpos")) {
					// Balance position behaviour module building here
					string[] splits = objLine.Split(' ');
					int vertexAttachIndex = int.Parse(splits[1]);
					float reduceRatio = float.Parse(splits[2]);
					GameObject balPolObj = Instantiate(BalancePositionInstantiator, this.transform);
					BMod_BalancePosition balPosMod = balPolObj.GetComponent<BMod_BalancePosition>();
					balPosMod.restrictionAreaShrinkRatio = reduceRatio;
					balPosMod.controlVertex = verticesBuild[vertexAttachIndex].GetComponent<RFrame_Vertex>();
					//balPosMod.controlVertex.lockedInPlace = true;
					balPosBuild.Add(balPolObj);
				} else if (objLine.StartsWith("dishol")) {
					// Distance holder behaviour module building here
					string[] splits = objLine.Split(' ');
					int balPosIndex = int.Parse(splits[1]);
					int vertexAttachIndex = int.Parse(splits[2]);
					float distanceToHold = float.Parse(splits[3]);
					int typeOfDisHol = int.Parse(splits[4]);
					GameObject disHolObj = Instantiate(DistanceHolderInstantiator, balPosBuild[balPosIndex].transform);
					BMod_DistanceHolder disHolMod = disHolObj.GetComponent<BMod_DistanceHolder>();
					disHolMod.vertexPoint = verticesBuild[vertexAttachIndex].GetComponent<RFrame_Vertex>();
					// Experimenting with not locking the vertex in place.
					// Need to make sure frame doesn't move out of place.
					disHolMod.vertexPoint.lockedInPlace = true;
					disHolMod.distanceToHold = distanceToHold;
					switch (typeOfDisHol) {
						case 0: disHolMod.moduleType = BMod_DistanceHolder.DistanceModuleType.groundDistance; break;
						case 1: disHolMod.moduleType = BMod_DistanceHolder.DistanceModuleType.allRoundDistance; break;
						default: disHolMod.moduleType = BMod_DistanceHolder.DistanceModuleType.groundDistance; break;
					}
					disHolBuild.Add(disHolObj);
				} else if (objLine.StartsWith("ancpoi")) {
					// Anchor Point behaviour module building here
					string[] splits = objLine.Split(' ');
					int disHolIndex = int.Parse(splits[1]);
					int vertexAttachIndex = int.Parse(splits[2]);
					int priorityValue = int.Parse(splits[3]);
					int typeOfAnchor = int.Parse(splits[4]);
					GameObject anchorObj = Instantiate(AnchorPointInstantiator, disHolBuild[disHolIndex].transform);
					BMod_AnchorPoint anchorMod = anchorObj.GetComponent<BMod_AnchorPoint>();
					anchorMod.anchorVertex = verticesBuild[vertexAttachIndex].GetComponent<RFrame_Vertex>();
					anchorMod.anchorVertex.lockedInPlace = true;
					anchorMod.priorityIndex = priorityValue;
					switch (typeOfAnchor) {
						case 0: anchorMod.anchorType = BMod_AnchorPoint.AnchorTypeEnum.primaryAnchor; break;
						case 1: anchorMod.anchorType = BMod_AnchorPoint.AnchorTypeEnum.secondaryAnchor; break;
						case 2: anchorMod.anchorType = BMod_AnchorPoint.AnchorTypeEnum.tertiaryAnchor; break;
						default: anchorMod.anchorType = BMod_AnchorPoint.AnchorTypeEnum.primaryAnchor; break;
					}
					ancPoiBuild.Add(anchorObj);
				}
			}
			// Set the parents of the objects instatiated to this object and
			// call the function for calculating all the distances between
			// struts.
			if (verticesBuild.Count!=0 && strutBuild.Count!=0) {
				// Apply the created vertices and struts to this object.
				// Set the parent of the vertices and struts to the transform of this frame object.
				// foreach (GameObject vertex in verticesBuild) {
				// 	vertex.transform.SetParent(this.transform);
				// }
				// foreach (GameObject strut in strutBuild) {
				// 	strut.transform.SetParent(this.transform);
				// }
				allVertices = verticesBuild;
				struts = strutBuild;
				angleLinks = angleViewBuild;
				compReps = compRepBuild;
				this.calculateDistancesThenOrientate();
				// Get the initial measured angles for each angle link.
				foreach(GameObject angleLinkObj in angleViewBuild) {
					angleLinkObj.GetComponent<RFrame_AngleLink>().measureAngle(true);
				}
				//frame.gatherBounds();
			}
		}
		
		static GameObject createStrutIfNotExisting(RFrame_Object frameObject, GameObject strutInstantiator, List<GameObject> strutsCheck, int v1, int v2) {
			RFrame_Strut strutToCheck;
			bool foundExistingStrut = false;
			//if (strutsCheck.Count==0) return false;
			foreach(GameObject strutToCheckGameObject in strutsCheck) {
				strutToCheck = strutToCheckGameObject.GetComponent<RFrame_Strut>();
				if(strutToCheck.v1==v1 && strutToCheck.v2==v2) {
					foundExistingStrut = true;
				}
				if(strutToCheck.v1==v2 && strutToCheck.v2==v1) {
					foundExistingStrut = true;
				}
			}
			if (!foundExistingStrut) {
				GameObject strutGameObject = Instantiate(strutInstantiator, frameObject.transform);
				RFrame_Strut newStrut = strutGameObject.GetComponent<RFrame_Strut>();
				newStrut.v1 = v1;
				newStrut.v2 = v2;
				strutsCheck.Add(strutGameObject);
				return strutGameObject;
			}
			return null;//foundExistingStrut;
		}
		
		void checkIfShortestStrut(RFrame_Strut strut) {
			if (shortestStrutLength==-1f || strut.length < shortestStrutLength) {
				shortestStrutLength = strut.length;
			}
		}
		
		public void calculateDistancesThenOrientate() {
			RFrame_Strut strutPull;
			foreach (GameObject strutObject in struts) {
				strutPull = strutObject.GetComponent<RFrame_Strut>();
				GameObject v1Obj = allVertices[strutPull.v1];
				GameObject v2Obj = allVertices[strutPull.v2];
				RFrame_Vertex v1 = v1Obj.GetComponent<RFrame_Vertex>();
				RFrame_Vertex v2 = v2Obj.GetComponent<RFrame_Vertex>();
				strutPull.length = Mathf.Abs(v1.distanceToVertex(v2, false)[0]);
				checkIfShortestStrut(strutPull);

				// Update the line renderers.
				Vector3[] positions = {v1Obj.transform.localPosition,v2Obj.transform.localPosition};
				strutPull.updateLineRenderPositions(positions);

			}
		}
		
		public float GetShortestStrutLength() {
			return shortestStrutLength;
		}

	}


}