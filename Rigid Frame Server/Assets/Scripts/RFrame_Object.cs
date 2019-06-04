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
		//float shortestStrutLength = -1f;
		
		public List<GameObject> allGameObjectVertices; // Array of all Fvertex objects. Each Fvertex object has an array of Fstruts
		public List<GameObject> allGameObjectStruts; // Array of GameObject Fstrut objects.
		public List<RFrame_Vertex> allVertices; // Array of all RFrame_Vertices.
		public List<RFrame_Strut> allStruts; // Array of RFrame_Struts.
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

		public float doubleVertexRoamDistance = 1.0f;

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
			
			List<GameObject> verticesGameObjectBuild = new List<GameObject>();
			List<GameObject> strutGameObjectBuild = new List<GameObject>();
			List<RFrame_Vertex> verticesBuild = new List<RFrame_Vertex>();
			List<RFrame_Strut> strutsBuild = new List<RFrame_Strut>();
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
					verticesGameObjectBuild.Add(vertexGameObject);
					verticesBuild.Add(vertexScript);
					vertexIndex++;
					
				} else if (objLine.StartsWith("l")) {
					// This is for any standalone lines, which will be turned into struts.
					// Modified to only work with the first two vertice indexes
					
					string[] splits = objLine.Split(' ');
					// Remember that indexes start at 1 in the obj/txt file and need converting to zero-indexed.
					int v1 = int.Parse(splits[1]) - 1;
					int v2 = int.Parse(splits[2]) - 1;
					GameObject strutGameObject = RFrame_Object.createStrutIfNotExisting(this, StrutInstantiator, strutGameObjectBuild, strutsBuild, v1, v2);
					// Check if strut is displayed. This is determined by having more than three entries in splits.
					// Basically any character entered after the 2 indexes will trigger this.
					//if(strutGameObject!=null && splits.Length > 3) {
					if (splits.Length==5 && strutGameObject!=null) {
						RFrame_Strut strutScript = strutGameObject.GetComponent<RFrame_Strut>();
						strutScript.minLength = float.Parse(splits[3]);
						strutScript.maxLength = float.Parse(splits[4]);
					}
					// All line renderers will be switched off.
					LineRenderer lineRenderer = strutGameObject.GetComponent<LineRenderer>();
					// Used when debugging the a fresh OBJ frame.
					lineRenderer.enabled = transform.GetComponentInParent<RFrame_Bridge>().displayFrameOnLoad;
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
					// Duplicate the position of the indexed vertex and create a strut inbetween.
					// The duplicated vertex is referenced by this Balance positioner only
					RFrame_Vertex attachToVertex = verticesBuild[vertexAttachIndex];
					GameObject vertexInstantObject = Instantiate(VertexInstantiator, this.transform); // Because Unity.
					RFrame_Vertex dupliVertex = vertexInstantObject.GetComponent<RFrame_Vertex>();
					dupliVertex.x = attachToVertex.x;
					dupliVertex.y = attachToVertex.y;
					dupliVertex.z = attachToVertex.z;
					dupliVertex.name = attachToVertex.name + "_Duplicate";
					vertexInstantObject.transform.localPosition = new Vector3(dupliVertex.x, dupliVertex.y, dupliVertex.z);
					dupliVertex.lockedInPlace = true; // The duplicated vertex is not allowed to roam
					GameObject strutInstantObject = Instantiate(StrutInstantiator, this.transform); // Because Unity.
					RFrame_Strut roamingStrut = strutInstantObject.GetComponent<RFrame_Strut>();
					roamingStrut.v1 = -1;
					roamingStrut.v2 = -1;
					roamingStrut.v1Ref = attachToVertex;
					roamingStrut.v2Ref = dupliVertex;
					roamingStrut.intendedLength = doubleVertexRoamDistance;
					roamingStrut.affirmLimit = 10; // This should allow a bit of roaming but eventually return to spot.
					strutsBuild.Add(roamingStrut);
					strutGameObjectBuild.Add(strutInstantObject);
					// Set the duplicated vertex to be moved by the balance positioner
					balPosMod.controlVertex = dupliVertex;
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

					// Add the duplicate vertex
					RFrame_Vertex attachToVertex = verticesBuild[vertexAttachIndex];
					GameObject vertexInstantObject = Instantiate(VertexInstantiator, this.transform); // Because Unity.
					RFrame_Vertex dupliVertex = vertexInstantObject.GetComponent<RFrame_Vertex>();
					dupliVertex.x = attachToVertex.x;
					dupliVertex.y = attachToVertex.y;
					dupliVertex.z = attachToVertex.z;
					vertexInstantObject.transform.localPosition = new Vector3(dupliVertex.x, dupliVertex.y, dupliVertex.z);
					dupliVertex.lockedInPlace = true; // The duplicated vertex is not allowed to roam
					GameObject strutInstant = Instantiate(StrutInstantiator, this.transform); // Because Unity.
					RFrame_Strut roamingStrut = strutInstant.GetComponent<RFrame_Strut>();
					roamingStrut.v1 = -1;
					roamingStrut.v2 = -1;
					roamingStrut.v1Ref = attachToVertex;
					roamingStrut.v2Ref = dupliVertex;
					roamingStrut.intendedLength = doubleVertexRoamDistance;
					roamingStrut.affirmLimit = 10; // This should allow a bit of roaming but eventually return to spot.
					strutsBuild.Add(roamingStrut);
					strutGameObjectBuild.Add(strutInstant);

					disHolMod.vertexPoint = dupliVertex;
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

					// Add the duplicate vertex
					RFrame_Vertex attachToVertex = verticesBuild[vertexAttachIndex];
					GameObject vertexInstantObject = Instantiate(VertexInstantiator, this.transform); // Because Unity.
					RFrame_Vertex dupliVertex = vertexInstantObject.GetComponent<RFrame_Vertex>();
					dupliVertex.x = attachToVertex.x;
					dupliVertex.y = attachToVertex.y;
					dupliVertex.z = attachToVertex.z;
					vertexInstantObject.transform.localPosition = new Vector3(dupliVertex.x, dupliVertex.y, dupliVertex.z);
					dupliVertex.lockedInPlace = true; // The duplicated vertex is not allowed to roam
					GameObject strutInstant = Instantiate(StrutInstantiator, this.transform); // Because Unity.
					RFrame_Strut roamingStrut = strutInstant.GetComponent<RFrame_Strut>();
					roamingStrut.v1 = -1;
					roamingStrut.v2 = -1;
					roamingStrut.v1Ref = attachToVertex;
					roamingStrut.v2Ref = dupliVertex;
					roamingStrut.intendedLength = doubleVertexRoamDistance;
					roamingStrut.affirmLimit = -1; // This should allow a bit of roaming but eventually return to spot.
					strutsBuild.Add(roamingStrut);
					strutGameObjectBuild.Add(strutInstant);

					anchorMod.anchorVertex = dupliVertex;
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
			if (verticesGameObjectBuild.Count!=0 && strutGameObjectBuild.Count!=0) {
				// Apply the created vertices and struts to this object.
				// Set the parent of the vertices and struts to the transform of this frame object.
				// foreach (GameObject vertex in verticesBuild) {
				// 	vertex.transform.SetParent(this.transform);
				// }
				// foreach (GameObject strut in strutBuild) {
				// 	strut.transform.SetParent(this.transform);
				// }
				allGameObjectVertices = verticesGameObjectBuild;
				allGameObjectStruts = strutGameObjectBuild;
				allVertices = verticesBuild;
				allStruts = strutsBuild;
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
		
		static GameObject createStrutIfNotExisting(RFrame_Object frameObject, GameObject strutInstantiator, List<GameObject> strutsCheck, List<RFrame_Strut> strutsRaw, int v1, int v2) {
			RFrame_Strut strutToCheck;
			foreach(GameObject strutToCheckGameObject in strutsCheck) {
				strutToCheck = strutToCheckGameObject.GetComponent<RFrame_Strut>();
				if(strutToCheck.v1==v1 && strutToCheck.v2==v2) {
					return strutToCheckGameObject;
				}
				if(strutToCheck.v1==v2 && strutToCheck.v2==v1) {
					return strutToCheckGameObject;
				}
			}
			// Create a new strut if this section is reached. Since an existing strut would have been returned.
			GameObject strutGameObject = Instantiate(strutInstantiator, frameObject.transform);
			RFrame_Strut newStrut = strutGameObject.GetComponent<RFrame_Strut>();
			newStrut.v1 = v1;
			newStrut.v2 = v2;
			strutsCheck.Add(strutGameObject);
			strutsRaw.Add(newStrut);
			return strutGameObject;
		}
		
		// void checkIfShortestStrut(RFrame_Strut strut) {
		// 	if (shortestStrutLength==-1f || strut.length < shortestStrutLength) {
		// 		shortestStrutLength = strut.length;
		// 	}
		// }
		
		public void calculateDistancesThenOrientate() {
			RFrame_Vertex v1;
			RFrame_Vertex v2;
			foreach (RFrame_Strut strutCalc in allStruts) {
				if (strutCalc.v1 != -1 && strutCalc.v2 != -1) {
					GameObject v1Obj = allGameObjectVertices[strutCalc.v1];
					GameObject v2Obj = allGameObjectVertices[strutCalc.v2];
					v1 = allVertices[strutCalc.v1];
					v2 = allVertices[strutCalc.v2];
					// Update the line renderers.
					Vector3[] positions = {v1Obj.transform.localPosition,v2Obj.transform.localPosition};
					strutCalc.updateLineRenderPositions(positions);
				} else {
					if (strutCalc.v1!=-1) {
						v1 = allVertices[strutCalc.v1];
					} else {
						v1 = strutCalc.v1Ref;
					}
					if (strutCalc.v2!=-1) {
						v2 = allVertices[strutCalc.v2];
					} else {
						v2 = strutCalc.v2Ref;
					}	
				}
				strutCalc.length = Mathf.Abs(v1.distanceToVertex(v2, false)[0]);
				//checkIfShortestStrut(strutCalc);
			}
		}
		
		// public float GetShortestStrutLength() {
		// 	return shortestStrutLength;
		// }

	}


}