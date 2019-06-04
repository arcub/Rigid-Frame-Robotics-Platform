using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RigidFrame_Development
{
	public class BMod_AnchorPoint : MonoBehaviour {
		public RFrame_Vertex anchorVertex; // This is the vertex the anchor point is controlling. This is slid around.
		public Vector3 lockedPosition; // A record of where the vertex should be locked. Does not slide.
		public Vector3 dropPosition; // This is the position the anchor is heading too
		public Vector3 unlockedPosition; // This is the point where the anchor point goes to when unlocked.
		public bool lockedInPosition = false; // When locked, this anchors vertex is slid by the BalancePosition module
		public bool needsToLift = false; // Indicates that this anchor point has reached its disconnect radius and needs to lift.
		public bool completedLockingSignal = false; // When true this indicated to the balance positioner that it needs to update.
		public float lockDropHeightKeepRatio = 0.99f; // For how much of the distance to keep above the ground.
		public float pushDownDistance = 1.0f; // How much to push into the ground when offset.
		public int priorityIndex = -1; // Should be greater than -1 for each anchor point.
		public Vector2 disconnectRadius = new Vector2(1.0f,2.0f); // Should be upgraded to elliptical
		public float movementlimitRadius = 4.0f;
		public Vector2 moveLimitRadiusVector;

		public GameObject dropPositionRepresentation;


		float previousLimitCheckDistance = 0.0f;
		public enum AnchorTypeEnum // For future purposes. Allows use of "elbows", but should overall be expanded to all "surface" vertices.
		{
			primaryAnchor,
			secondaryAnchor,
			tertiaryAnchor
		}

		public enum AnchorCurrentActionEnum
		{
			idle,
			movingToStartingLockPosition,
			movingToUnlockPosition,
			movingToDesinationPosition,
			waitingForBalance
		}
		public AnchorTypeEnum anchorType = AnchorTypeEnum.primaryAnchor;
		public AnchorCurrentActionEnum currentAction = AnchorCurrentActionEnum.idle;
		// Use this for initialization
		void Start () {
			// Build the limit radius using the disconnect radius
			moveLimitRadiusVector = disconnectRadius * movementlimitRadius;
			// Draw the disconnect radius
			LineRenderer lineRenderer = transform.GetComponent<LineRenderer>();
			if(lineRenderer!=null) {
				List<Vector3> ellipse = new List<Vector3>();
				int maxPoints = 20;
				float perPointAngle = 360.0f / maxPoints;
				for (int i = 0; i < maxPoints; i++) {
					float xVal = lockedPosition.x + moveLimitRadiusVector.x * Mathf.Cos(perPointAngle * i * Mathf.Deg2Rad);
					float yVal = lockedPosition.y + 0.2f;
					float zVal = lockedPosition.z + moveLimitRadiusVector.y * Mathf.Sin(perPointAngle * i * Mathf.Deg2Rad);
					Vector3 point = new Vector3(xVal, yVal, zVal);
					ellipse.Add(point);
				}
				lineRenderer.positionCount = ellipse.Count;
				lineRenderer.SetPositions(ellipse.ToArray());
			}
			
		}
		
		// Update is called once per frame
		void Update () {
			checkPositionStatus();
		}

		void checkPositionStatus() {
			// Method for checking where this anchor point is in relation to where it should be.
			switch(currentAction) {
				case AnchorCurrentActionEnum.idle: {
					// Check if slide position has moved out of bounds of the distance ellipse.
					BMod_BalancePosition balPos = transform.parent.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						if (balPos.balPosWorkingStatus==BMod_BalancePosition.balPolStatusEnum.readyForWork) {
							Vector3 fromLockedToSliding = lockedPosition - anchorVertex.transform.position;
							Vector3 fromLastDropPoint = dropPosition - anchorVertex.transform.position;
							float angleFromLeft = Vector3.Angle(fromLockedToSliding, Vector3.left);
							float distanceForAngle = disconnectDistanceForAngle(angleFromLeft);

							// Apply ground pushing bias based on distance from locked to current
							// float limitDist = limitDistanceForAngle(angleFromLeft);
							// float ratioToLimit = 0.0f;
							// if (fromLockedToSliding.magnitude==0.0f) {
							// 	ratioToLimit = 1.0f;
							// } else {
							// 	ratioToLimit = 1.0f - (fromLockedToSliding.magnitude / limitDist);
							// 	//Debug.Log(limitDist + " Limit distance, " + fromLockedToSliding.magnitude + " Mag, Limit distance ratio " + ratioToLimit);
							// }
							// if (fromLockedToSliding.magnitude > limitDist) {
							// 	ratioToLimit = 0.0f;
							// }
							// float groundPush = pushDownDistance * balPos.noncumulativeOffset.magnitude * ratioToLimit;
							// Vector3 newPos = new Vector3(anchorVertex.transform.position.x, -groundPush, anchorVertex.transform.position.z);
							// anchorVertex.transform.position = newPos;
							// //anchorVertex.x = anchorVertex.transform.localPosition.x;
							// anchorVertex.y = anchorVertex.transform.localPosition.y;
							//anchorVertex.z = anchorVertex.transform.localPosition.z;

							//Debug.Log("Anchor " + priorityIndex + " angle " + angleFromLeft + " distance " + distanceForAngle);
							//float xDebug = lockedPosition.x + 2.0f * Mathf.Cos(angleFromLeft*Mathf.Deg2Rad);
							//float yDebug = lockedPosition.y + 0.3f;
							//float zDebug = lockedPosition.z + 2.0f * Mathf.Sin(angleFromLeft * Mathf.Deg2Rad);
							// Debug.DrawLine(lockedPosition, new Vector3(xDebug, yDebug, zDebug));
							// Need to take into account that last drop position will be outside the last drop point.
							// This codes needs to be more robust.
							// MAYBE have unlock area change to encompass drop point.
							// Attempting to use a standard deviation threshold is drop point and lock
							// points differ.
							if(lockedPosition!=dropPosition) {
								float ld = (dropPosition - lockedPosition).magnitude;
								float cl = (lockedPosition - anchorVertex.transform.position).magnitude;
								float clcd = (dropPosition - anchorVertex.transform.position).magnitude + cl;
								float average = (ld + clcd) / 2.0f;
								float dev = Mathf.Sqrt((Mathf.Pow(ld-average,2)+Mathf.Pow(clcd-average,2))/2);
								//Debug.Log("Deviation " + dev);
								if(dev > balPos.lockDropDeviation) {
									needsToLift = true;
								} else {
									needsToLift = false;
								}
							} else {
								if(fromLockedToSliding.magnitude > distanceForAngle) {
									needsToLift = true;
								} else {
									// This will happen in case anchor points which haven't lifted yet no longer need to lift.
									needsToLift = false;
								}
							}
						}
					}
					break;
				}
				case AnchorCurrentActionEnum.movingToStartingLockPosition: {
					// 1 - Collect bias acceleration and apply to movement of vertex
					// 2 - Check if destination reached
					BMod_BalancePosition balPos = transform.parent.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						//if (balPos.readyForWork) {
							Vector3 newPos = Vector3.MoveTowards(anchorVertex.transform.position, lockedPosition, balPos.maxSlideMovementSpeed*Time.deltaTime);
							anchorVertex.transform.position = newPos;
							anchorVertex.x = anchorVertex.transform.localPosition.x;
							anchorVertex.y = anchorVertex.transform.localPosition.y;
							anchorVertex.z = anchorVertex.transform.localPosition.z;
							// Check if destination has been reached
							if (Vector3.Distance(newPos, lockedPosition)==0.0f) {
								lockedInPosition = true;
								currentAction = AnchorCurrentActionEnum.idle;
							}
						//}
					}
					break;
				}
				case AnchorCurrentActionEnum.movingToUnlockPosition: {
					// Collect bias acceleration and apply to movement of vertex
					// Check if destination reached
					BMod_BalancePosition balPos = transform.parent.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						if (balPos.balPosWorkingStatus==BMod_BalancePosition.balPolStatusEnum.readyForWork) {
							Vector3 newPos = Vector3.MoveTowards(anchorVertex.transform.position, unlockedPosition, balPos.maxSlideMovementSpeed*Time.deltaTime);
							anchorVertex.transform.position = newPos;
							anchorVertex.x = anchorVertex.transform.localPosition.x;
							anchorVertex.y = anchorVertex.transform.localPosition.y;
							anchorVertex.z = anchorVertex.transform.localPosition.z;

							// Check if destination has been reached
							if (Vector3.Distance(newPos, unlockedPosition)==0.0f) {
								currentAction = AnchorCurrentActionEnum.movingToDesinationPosition;
							}
						}
					}
					break;
				}
				case AnchorCurrentActionEnum.movingToDesinationPosition: {
					// Collect bias acceleration and apply to movement of vertex
					// Check if destination reached
					// The drop position is dynamic and linked to the direction of the main offset & rotation of the balance positioner
					BMod_BalancePosition balPos = transform.parent.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						if (balPos.balPosWorkingStatus==BMod_BalancePosition.balPolStatusEnum.readyForWork) {
							// Update the drop position based on the offset direction of the balance positioner and
							// magnitude of non-cumulative offset. It is still slid around by the overall offset
							Vector3 offsetDirection = balPos.noncumulativeOffset.normalized; // balPos.slideOffset.normalized;
							// Swap the x and z around
							float angleFromLeft = Vector3.Angle(offsetDirection, Vector3.left);
							float distanceForAngle = disconnectDistanceForAngle(angleFromLeft);
							float dropDistance = balPos.noncumulativeOffset.magnitude * (distanceForAngle*balPos.maxStepDistance);
							dropPosition = lockedPosition + (-offsetDirection * dropDistance);
							// Update the position of any drop representors.
							if(dropPositionRepresentation!=null) {
								dropPositionRepresentation.transform.position = dropPosition;
							}
							// Move delta distance to destination. The move speed is based on the noncumulative speed.
							float noncumulativeMag = balPos.noncumulativeOffset.magnitude;
							if( noncumulativeMag == 0.0f) {
								noncumulativeMag = 1.0f;
							}
							//float amountToMove = noncumulativeMag * balPos.maxSlideMovementSpeed*Time.deltaTime;
							// Legs should move faster than slide speed
							float amountToMove = 2.0f * balPos.maxSlideMovementSpeed*Time.deltaTime;
							
							Vector3 newPos = Vector3.MoveTowards(anchorVertex.transform.position, dropPosition, amountToMove);
							
							// Drop distance for the level with drop position
							Vector3 newPosLevel = new Vector3(newPos.x, dropPosition.y, newPos.z);

							// Interim code. Keep leg lifted until close to drop point.
							// Updated so the leg is held high if it's over a certain distance away form the lock point.
							float lockToDropDist = Vector3.Distance(lockedPosition, dropPosition);
							float levelToDropDist = Vector3.Distance(newPosLevel, dropPosition);
							if(levelToDropDist > lockToDropDist*lockDropHeightKeepRatio && lockToDropDist != 0.0f) {
								newPos.y = balPos.anchorLiftHeight;
							}
							// float xDebug = lockedPosition.x + 2.0f * Mathf.Cos(angleFromLeft * Mathf.Deg2Rad);
							// float yDebug = lockedPosition.y + 0.3f;
							// float zDebug = lockedPosition.z + 2.0f * Mathf.Sin(angleFromLeft * Mathf.Deg2Rad);
							
							// Debug.DrawLine(lockedPosition, new Vector3(xDebug, yDebug, zDebug));
							
							anchorVertex.transform.position = newPos;
							anchorVertex.x = anchorVertex.transform.localPosition.x;
							anchorVertex.y = anchorVertex.transform.localPosition.y;
							anchorVertex.z = anchorVertex.transform.localPosition.z;
							// Check if destination has been reached within a threshold value
							float distanceToDrop = Vector3.Distance(newPos, dropPosition);
							float threshold = balPos.dropDistanceThreshold;
							if (distanceToDrop <= threshold) {
								currentAction = AnchorCurrentActionEnum.idle;
								//lockedPosition = dropPosition;
								lockedInPosition = true;
								completedLockingSignal = true;
							}
						}
					}
					break;
				}
				case AnchorCurrentActionEnum.waitingForBalance: {
					// This is for a leg has been given approval for lifting, but is waiting for balance to be reached.
					BMod_BalancePosition balPos = transform.parent.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						// If the balance hsa been reached then this anchor can be unlocked to move to unlock position.
						if (balPos.balPosWorkingStatus==BMod_BalancePosition.balPolStatusEnum.readyForWork) {
							lockedInPosition = false;
							currentAction = AnchorCurrentActionEnum.movingToUnlockPosition;
						}
					}
					break;
				}
			}
		}

		float disconnectDistanceForAngle(float degrees) {
			float xVal = disconnectRadius.x * Mathf.Cos(degrees * Mathf.Deg2Rad);
			float zVal = disconnectRadius.y * Mathf.Sin(degrees * Mathf.Deg2Rad);
			return Mathf.Sqrt(xVal*xVal+zVal*zVal);
		}

		float limitDistanceForAngle(float degrees) {
			float xVal = moveLimitRadiusVector.x * Mathf.Cos(degrees * Mathf.Deg2Rad);
			float zVal = moveLimitRadiusVector.y * Mathf.Sin(degrees * Mathf.Deg2Rad);
			return Mathf.Sqrt(xVal*xVal+zVal*zVal);
		}

		public Vector3 limitReachCorrectionVector() {
			// Compare the curent position with the lock position
			// Keeping the nonHeightAnchor correction.
			Vector3 nonHeightAnchor = new Vector3(anchorVertex.transform.position.x, 0.0f, anchorVertex.transform.position.z);
			Vector3 fromLockedToSliding = lockedPosition - nonHeightAnchor;
			float angleFromLeft = Vector3.Angle(fromLockedToSliding, Vector3.left);
			float distanceForAngle = limitDistanceForAngle(angleFromLeft);
			//Debug.Log("Anchor " + priorityIndex + " angle " + angleFromLeft + " distance " + distanceForAngle);
			// float xDebug = lockedPosition.x + 2.0f * Mathf.Cos(angleFromLeft*Mathf.Deg2Rad);
			// float yDebug = lockedPosition.y + 0.3f;
			// float zDebug = lockedPosition.z + 2.0f * Mathf.Sin(angleFromLeft * Mathf.Deg2Rad);
			// // Debug.DrawLine(lockedPosition, new Vector3(xDebug, yDebug, zDebug));
			Vector3 correctionOffset = new Vector3();
			float distanceToCheck = fromLockedToSliding.magnitude;
			// By comparing the distance from the previous check this should reduce the amount
			// of tiptoeing or restricted movement due to stepping outside of the limit, but moving
			// back in.
			if(distanceToCheck > distanceForAngle && distanceToCheck > previousLimitCheckDistance) {
				// Create a reversed vector that prevent the roaming outside the limit.
				correctionOffset = fromLockedToSliding.normalized * (fromLockedToSliding.magnitude - distanceForAngle); 
			}
			previousLimitCheckDistance = fromLockedToSliding.magnitude;
			return correctionOffset;
		}

	}

}