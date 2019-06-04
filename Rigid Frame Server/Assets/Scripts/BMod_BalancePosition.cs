using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RigidFrame_Development
{
	public class BMod_BalancePosition : MonoBehaviour {
		// Behaviour module - Balance Position
		// Key module that is used for positioning the rigid frame and
		// acts as control for distance holder and anchor point modules.
		BMod_DistanceHolder[] distanceHolders; // Distance holders should be in order to form a perimeter, no cross-overs.
		BMod_AnchorPoint[] anchorPoints;
		
		public Vector3 previousSlideOffset; // Use to detect when the slide vectors need updating.
		public Vector3 slideOffset; // The amount of the offset from this balance positions vertex to move.
		public Vector3 noncumulativeOffset; // This is used to determine how far away the drop point is from the original anchor lock point.
		public Vector3 biasCentreTrack; // This is where input for movement is placed. This can be considered as acceleration.
		public GameObject biasCentreRepresentation; // An object to represent the movement centre.
		public float rotationDegreeDelta = 1.5f;
		
		public float crouchRange = 4.0f;
		public RFrame_Vertex controlVertex; // This is the vertex that acts as the rotation point and which is locked in place.
		[Tooltip("Ratio of the size of the area to keep the centre inside.")]
		public float restrictionAreaShrinkRatio = 0.4f;
		public float dropAreaRatio = 1.0f;
		//[Tooltip ("Distance per update to move for limbs")]
		//public float movementDelta = 0.3f;
		
		//[Tooltip("Multiple of movement delta for anchor movement")]
		//public float anchorMoveMultplier = 2.5f;

		[Tooltip("The threshold deviation from the line between the lock and drop positions of an anchor point. This triggers the anchor to lift when the drop point is outside the original lock point range.")]
		public float lockDropDeviation = 0.3f;

		[Tooltip("Distance gap from drop point for allowable dropping")]
		public float dropDistanceThreshold = 0.1f;
		[Tooltip("Number of anchors to release in one time")]
		public int maxReleasable = 1;
		public List<Vector3> biasMovementRestrictionArea; // The bias movement vector will be pushed back into this region.
		public List<Vector3> proposedMovementRestrictionArea; // If this exists it will override the biasMovementRestrictionArea variable.
		[Tooltip("Maximum speed of sliding of anchors and other moveables. This is time delta limited.")]
		public float maxSlideMovementSpeed = 5.0f;
		public float anchorLiftHeight = 1.5f;
		public float maxStepDistance = 2.0f;
		bool restrictedMovementAreaUpdateRequired = false;
		
		public enum balPolStatusEnum {
			readyForWork,
			adjustingBalance,
			startingUp
		}
		
		public balPolStatusEnum balPosWorkingStatus = balPolStatusEnum.startingUp; // This is set to true after the startUpSequence has completed. BMods under this BMod check here first.
		// Use this for initialization
		void Start () {
			// Collect all the needed stuff.
			startUpSequence();
			//readyForWork = true; // Does this need to be moved into the startUpSequence function?
		}
		
		// Update is called once per frame
		void Update () {
			// TODO Balance position needs to be upgraded to use a centre of balance
			// system, where the body is tilted and shifted as needed.
			// Process each of the distance holder and anchor points attached to this
			// class.
			if(restrictedMovementAreaUpdateRequired) {
				updateRestrictionRegion(false);
				restrictedMovementAreaUpdateRequired = false;
			}

			// Control pad movement
			float hozAxis = Input.GetAxis("Horizontal") * maxSlideMovementSpeed;
			float verAxis = Input.GetAxis("Vertical") * maxSlideMovementSpeed;
			noncumulativeOffset.x = Input.GetAxis("Horizontal");
			noncumulativeOffset.z = Input.GetAxis("Vertical");
			float keyBiasOffset = 0.5f; // This goes towards how far forward the anchor point
			// is positioned in the direction of movement for keyboard based control.
			// Collect bias from external controls
			if(Input.GetKey(KeyCode.UpArrow)) {
				slideOffset.z = slideOffset.z + (maxSlideMovementSpeed * Time.deltaTime);
				noncumulativeOffset.z = keyBiasOffset;
				noncumulativeOffset.x = 0.0f;
			}
			if (Input.GetKey(KeyCode.DownArrow)){
				slideOffset.z = slideOffset.z - (maxSlideMovementSpeed * Time.deltaTime);
				noncumulativeOffset.z = -keyBiasOffset;
				noncumulativeOffset.x = 0.0f;
			}
			if (Input.GetKey(KeyCode.LeftArrow)) {
				slideOffset.x = slideOffset.x + (maxSlideMovementSpeed * Time.deltaTime);
				noncumulativeOffset.z = 0.0f;
				noncumulativeOffset.x = keyBiasOffset;
			}
			if (Input.GetKey(KeyCode.RightArrow)) {
				slideOffset.x = slideOffset.x - (maxSlideMovementSpeed * Time.deltaTime);
				noncumulativeOffset.z = 0.0f;
				noncumulativeOffset.x = -keyBiasOffset;
			}
			
			// Controls switches
			bool isLeaningSwitch = Input.GetButton("Button_R");
			bool isNoLegLiftingSwitch = Input.GetButton("Button_L");

			// Manual leg lifting button presses
			int anchorLift = -1;
			if (Input.GetButtonDown("Button_A")) {
				anchorLift = 0;
			}
			if (Input.GetButtonDown("Button_B")) {
				anchorLift = 1;
			}
			if (Input.GetButtonDown("Button_X")) {
				anchorLift = 3;
			}
			if (Input.GetButtonDown("Button_Y")) {
				anchorLift = 2;
			}
			if (anchorLift != -1) {
				BMod_AnchorPoint anchorPoint = anchorPoints[anchorLift];
				anchorPoint.needsToLift = true;
			}


			//Debug.Log("Hoz " + hozAxis + ", Ver " + verAxis);
			// Only apply horizontal and vertical slide axis if bal pos is ready for work
			if(balPosWorkingStatus==balPolStatusEnum.readyForWork) {
				slideOffset.x = slideOffset.x + (hozAxis * Time.deltaTime);
				slideOffset.z = slideOffset.z + (verAxis * Time.deltaTime);
			}
			// Rotation around centre point slide calculations here
			float rotAxis = Input.GetAxis("Hoz Rotate") * rotationDegreeDelta;
			
			// Calculate the restriction centre and compare with the actual roving centre.
			Vector3 biasActualCentre = new Vector3();
			foreach(Vector3 biasVector in biasMovementRestrictionArea) {
				biasActualCentre = biasActualCentre + biasVector;
			}
			biasActualCentre = biasActualCentre / biasMovementRestrictionArea.Count;
			if (Vector3.Distance(biasCentreTrack, biasActualCentre)> 0.0f) {
				// If the centre if off then the robot is adjusting its balance
				balPosWorkingStatus = balPolStatusEnum.adjustingBalance;
				Vector3 biasMoved = Vector3.MoveTowards(biasCentreTrack, biasActualCentre, maxSlideMovementSpeed * Time.deltaTime);
				slideOffset = slideOffset + (biasCentreTrack - biasMoved) * 1.2f; // Boost the movement
				// Move the bias centre.
				biasCentreTrack = biasCentreTrack - (biasCentreTrack - biasMoved);
				if(biasCentreRepresentation!=null) {
					biasCentreRepresentation.transform.position = biasCentreTrack;
				}
			} else {
				// If the balance point has been reached then declare ready for work, unless still in the start up sequence.
				if(balPosWorkingStatus!=balPolStatusEnum.startingUp) {
					balPosWorkingStatus = balPolStatusEnum.readyForWork;
				}
			}

			// Roll through the anchor points and collect the limit reached offset vectices
			foreach(BMod_AnchorPoint anchorPoint in anchorPoints) {
				// Only pull from the ones locked in place
				if (anchorPoint.lockedInPosition) {
					Vector3 limitCorrection = anchorPoint.limitReachCorrectionVector();
					slideOffset.x = slideOffset.x + limitCorrection.x;
					slideOffset.z = slideOffset.z + limitCorrection.z;
				}
			}

			// Update position sliders (Anchor points for now) with acceleration bias.
			// Only allow movement when balanced.
			if ((previousSlideOffset != slideOffset || rotAxis != 0.0f) && !isLeaningSwitch  && anchorPoints != null) {
				// Apply the slide and rotation changes to the anchor points.
				float angleRad = rotAxis*rotationDegreeDelta*Mathf.Deg2Rad; // Changed rotationDegreeDelta to movementSpeed
				foreach(BMod_AnchorPoint anchorPoint in anchorPoints) {
					if (anchorPoint.lockedInPosition) {
						// Only apply offset as translation difference from previous offset if the anchor is locked.
						Vector3 rotationOffset = Vector3.zero;
						if(rotAxis!=0.0f) {
							// Rotation offset
							Vector3 currentPosition = anchorPoint.anchorVertex.transform.position;
							// The current position should be a vector from x = 0, z = 0. So be applying a rotation
							// to the current position and keeping the offset, this should work as a way of creating rotation.
							float xVal = currentPosition.x * Mathf.Cos(angleRad) - currentPosition.z * Mathf.Sin(angleRad);
							float yVal = currentPosition.y; // We're not changing the height. Future versions may need to height check.
							float zVal = currentPosition.x * Mathf.Sin(angleRad) + currentPosition.z * Mathf.Cos(angleRad);
							rotationOffset = (new Vector3(xVal,yVal,zVal)) - currentPosition;
						}
						// Apply the changes as an offset
						anchorPoint.anchorVertex.x += (slideOffset.x - previousSlideOffset.x) + rotationOffset.x;
						anchorPoint.anchorVertex.y += (slideOffset.y - previousSlideOffset.y) + rotationOffset.y;
						anchorPoint.anchorVertex.z += (slideOffset.z - previousSlideOffset.z) + rotationOffset.z;
						//anchorPoint.anchorVertex.offset += (slideOffset - previousSlideOffset) + rotationOffset; // Apply the change in offset
					}
				}
				previousSlideOffset = slideOffset;
			}

			// Check if any anchors have reached thier drop points.
			checkForCompletedAnchors();

			// If the counter bias has zero movement then any anchor points waiting for
			// release can be processed.
			// Check if prevent lift button is not pressed
			if (!isNoLegLiftingSwitch) {
				checkForReleaseableAnchors();
			} //else {
			// 	Debug.Log("Button R pressed held");
			// }

			// Adjust distance holder offsets
			float crouchAxis = Input.GetAxis("Crouch") * crouchRange;
			if(distanceHolders!=null) {
				if(isLeaningSwitch) {
					// This is to allow the rotational leaning of the robot.
					Vector3 leanAxis = new Vector3(-Input.GetAxis("Hoz Rotate"), 0.0f, Input.GetAxis("Crouch"));
					leanAxis = leanAxis * 1.5f; // Increase the movement range
					foreach(BMod_DistanceHolder distanceHolder in distanceHolders) {
						Vector3 distHold = distanceHolder.vertexPoint.transform.position;
						Vector3 groundHolder = new Vector3(distHold.x, 0.0f, distHold.z);
						float distToCentre = Vector3.Distance(biasActualCentre, groundHolder);
						float distToTracked = Vector3.Distance(biasActualCentre+leanAxis, groundHolder);
						float ratioAlt = distToTracked / distToCentre;
						float tiltOffset = 0.0f;
						if (ratioAlt != 1.0) {
							tiltOffset = distanceHolder.distanceToHold - (distanceHolder.distanceToHold * ratioAlt);
						}
						distanceHolder.distanceToHoldOffset = tiltOffset;
					}
				} else {
					// Alter the height of each distance holder by the crouch axis
					foreach(BMod_DistanceHolder distanceHolder in distanceHolders) {
						distanceHolder.distanceToHoldOffset = crouchAxis;
					}
				}
			}


		}


		// This method is for when the system starts up fresh and hasn't
		// positioned the legs in any way.
		// 1 Enumerate the Distance holders into an array.
		// 2 Project thier position onto the ground plane/surface
		// 3 Use the projections to form a movement restriction region
		// 4 Apply paired anchor points to project positions.
		// 5 Calculate anchor position radii
		void startUpSequence() {
			// Collect distance holders
			if (distanceHolders==null) {
				distanceHolders = GetComponentsInChildren<BMod_DistanceHolder>();
			}
			// Proceed if there are distance holders defined
			updateRestrictionRegion(true);
			// Place anchor points at the projected ground positions for each anchor points parent distance holder
			int layerMask = 1 << 8;
			RaycastHit strikePoint;
			Vector3 centerPoint = new Vector3();
			bool centerPointFound = false;
			// Project the attached vertex position down to the ground as a centre point.
			if(Physics.Raycast(controlVertex.transform.position, -Vector3.up, out strikePoint, 200.0f, layerMask)) {
				centerPoint = strikePoint.point;
				centerPointFound = true;
				biasCentreTrack = centerPoint; // Update the roving centre point.
			}
			List<BMod_AnchorPoint> anchorList = new List<BMod_AnchorPoint>();
			foreach(BMod_DistanceHolder distanceHolder in distanceHolders) {
				// Get the anchor point attached to this distance holder
				BMod_AnchorPoint anchorPoint = distanceHolder.transform.GetComponentInChildren<BMod_AnchorPoint>();
				if(anchorPoint!=null) {
					anchorList.Add(anchorPoint);
					// Set down the anchor points to be scaled out slightly from the centre point
					if (centerPointFound) {
						Vector3 anchorPosition = ((distanceHolder.projectedGroundPosition - centerPoint) * dropAreaRatio) + centerPoint;
						
						anchorPoint.lockedPosition = anchorPosition;
					} else {
						anchorPoint.lockedPosition = distanceHolder.projectedGroundPosition;
					}
					anchorPoint.dropPosition = anchorPoint.lockedPosition;
					anchorPoint.unlockedPosition = distanceHolder.preferredUnlockPosition;
					anchorPoint.currentAction = BMod_AnchorPoint.AnchorCurrentActionEnum.movingToStartingLockPosition;
				}
			}
			if (anchorList.Count > 0) {
				anchorPoints = anchorList.ToArray();
			}
		}

		void updateRestrictionRegion(bool startUpSequence) {
			if (distanceHolders!=null) {
				// Call their project routines
				// Collect each projected distance holder point into a array.
				List<Vector3> collectVectors = new List<Vector3>();
				Vector3 sumVector = new Vector3();
				foreach(BMod_DistanceHolder distanceHolder in distanceHolders) {
					distanceHolder.updateProjectionValues();
					if (startUpSequence) {
						collectVectors.Add(distanceHolder.projectedGroundPosition);
						sumVector = sumVector + distanceHolder.projectedGroundPosition;
					} else {
						// Use anchor point if locked.
						BMod_AnchorPoint anchorPoint = distanceHolder.transform.GetComponentInChildren<BMod_AnchorPoint>();
						if(anchorPoint!=null) {
							if(anchorPoint.lockedInPosition && anchorPoint.currentAction!=BMod_AnchorPoint.AnchorCurrentActionEnum.waitingForBalance) {
								collectVectors.Add(anchorPoint.lockedPosition);
								sumVector = sumVector + anchorPoint.lockedPosition;
							}
						}
					}
					
				}
				// Form restriction region from collected ground positions.
				// Should an actual 3D object be created that's really tall?
				Vector3 centrePoint = sumVector / collectVectors.Count;
				biasMovementRestrictionArea = new List<Vector3>();
				// Shrink each vector towards the centre point.
				foreach(Vector3 workVector in collectVectors) {
					Vector3 scaledVector = ((workVector - centrePoint) * restrictionAreaShrinkRatio) + centrePoint;
					biasMovementRestrictionArea.Add(scaledVector);
				}
				// Build a line renderer
				LineRenderer debugLineDraw = GetComponent<LineRenderer>();
				if(debugLineDraw!=null) {
					Vector3[] positions = biasMovementRestrictionArea.ToArray();
					debugLineDraw.positionCount = biasMovementRestrictionArea.Count; // Set position count first
					debugLineDraw.SetPositions(positions);
				}
			}
		}

		void checkForReleaseableAnchors() {
			// Cycle through anchor points to check for any that need to be released.
			// Priority given to those furthest away from this balance position, which any tied then on priority value.
			if(anchorPoints!=null) {
				BMod_AnchorPoint anchorToUnlock = null;
				int unlockedCount = 0;
				List<BMod_AnchorPoint> requestors = new List<BMod_AnchorPoint>();
				foreach(BMod_AnchorPoint anchor in anchorPoints) {
					if(anchor.needsToLift) {
						requestors.Add(anchor);
					}
					if(anchor.lockedInPosition==false || anchor.currentAction==BMod_AnchorPoint.AnchorCurrentActionEnum.waitingForBalance) {
						unlockedCount++;
					}
				}
				// Only proceed if there's less anchor points unlocked than the maximum allowed.
				if(unlockedCount<maxReleasable && requestors.Count>0) {
					if(requestors.Count>maxReleasable) {
						// Filter by distance and then by priority if needed.
						// Distance is how far from original lock position
						// Use a dictionary with the distance rounded to whole number. If a duplicat entry exists,
						// it is overriden if the duplcate has a lower priority level.
						// Dictionary is then sorted highest to lowest and first entry taken out.
						Dictionary<int, BMod_AnchorPoint> distanceAnchors = new Dictionary<int, BMod_AnchorPoint>();
						foreach (BMod_AnchorPoint anchorCheck in requestors) {
							// Only working with ground height of zero for the distances for now.
							// Was used originally as part of the pushing through the ground test.
							Vector3 nonHeightAnchor = new Vector3(anchorCheck.anchorVertex.transform.position.x, 0.0f, anchorCheck.anchorVertex.transform.position.z);
							int distanceToOriginalLockPoint = Mathf.RoundToInt(Vector3.Distance(anchorCheck.lockedPosition, nonHeightAnchor)*10.0f);
							if(distanceAnchors.ContainsKey(distanceToOriginalLockPoint)) {
								if(distanceAnchors[distanceToOriginalLockPoint].priorityIndex < anchorCheck.priorityIndex) {
									// Replace original anchor point
									distanceAnchors[distanceToOriginalLockPoint] = anchorCheck;
								}
							} else {
								distanceAnchors.Add(distanceToOriginalLockPoint, anchorCheck);
							}
						}
						// Loop through keys and find the largest distance
						int maxDistanceFound = -99;
						foreach (int keyEntry in distanceAnchors.Keys) {
							if (keyEntry > maxDistanceFound) {
								maxDistanceFound = keyEntry;
							}
						}
						// If a distance was found that wasn't the initialiser number then assign for unlocking.
						if(maxDistanceFound!=-99) {
							anchorToUnlock = distanceAnchors[maxDistanceFound];
						}
					} else {
						// Allow the anchor to be unlocked.
						anchorToUnlock = requestors[0];
					}
				}
				// If an anchor point was found to release, then unlock it.
				// Safety check status will need to be implemented to allow centre point time to move
				// within restriction zone if restricted area shrinks past it.
				if(anchorToUnlock!=null) {
					anchorToUnlock.currentAction = BMod_AnchorPoint.AnchorCurrentActionEnum.waitingForBalance;
					balPosWorkingStatus = balPolStatusEnum.adjustingBalance; // Make sure this is called.
					//anchorToUnlock.lockedInPosition = false;
					anchorToUnlock.needsToLift = false;
					Vector3 nonHeightAnchor = new Vector3(anchorToUnlock.anchorVertex.transform.position.x, 0.0f, anchorToUnlock.anchorVertex.transform.position.z);
					Vector3 currentPosition = nonHeightAnchor;
					// Update the unlock position to be slightly above the current position
					anchorToUnlock.unlockedPosition = new Vector3(currentPosition.x, currentPosition.y+anchorLiftHeight, currentPosition.z);
					restrictedMovementAreaUpdateRequired = true;
				}
			}

		}


		void checkForCompletedAnchors() {
			// If any anchors have reached their drop points, then trigger a restriction zone rebuild
			int readyForWorkIdleCheckCount = 0;
			if(anchorPoints!=null) {
				foreach(BMod_AnchorPoint anchorPoint in anchorPoints) {
					if(anchorPoint.completedLockingSignal) {
						restrictedMovementAreaUpdateRequired = true;
						anchorPoint.completedLockingSignal = false; // Reset the signal
					}
					if(balPosWorkingStatus==balPolStatusEnum.startingUp) {
						// Part of start up sequence. Due to the rigid frame not being in a suitable
						// position on loading, everything must wait until the anchorpoints have
						// reached their lock positions before anything is sent to the linked frame.
						if(anchorPoint.currentAction==BMod_AnchorPoint.AnchorCurrentActionEnum.idle) {
							readyForWorkIdleCheckCount++;
						}
					}
				}
				// If all the anchor points are idle, then the startup sequence is complete.
				if(balPosWorkingStatus==balPolStatusEnum.startingUp) {
					if(readyForWorkIdleCheckCount==anchorPoints.Length) {
						balPosWorkingStatus = balPolStatusEnum.readyForWork;
					}
				}
			}
		}
	}

}