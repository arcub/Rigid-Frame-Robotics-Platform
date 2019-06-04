using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RigidFrame_Development
{

	public class BMod_DistanceHolder : MonoBehaviour {
		public enum DistanceModuleType
		{
			groundDistance,
			allRoundDistance
		};
		public DistanceModuleType moduleType = DistanceModuleType.groundDistance;

		public RFrame_Vertex vertexPoint;
		public Vector3 projectedGroundPosition;
		public Vector3 preferredUnlockPosition;
		public float distanceToHold = 16.0f;
		public float distanceToHoldOffset = 0.0f;
		public float currentDistance = 0.0f;
		// Use this for initialization
		void Start () {
			
		}
		
		// Update is called once per frame
		void Update () {
			// Code for adjusting height here.
			// Currently supports ground only type
			// First check height
			float distanceRequired = distanceToHold + distanceToHoldOffset;
			
			RaycastHit strikePoint;
			int layerMask = 1 << 8;
			if(vertexPoint!=null) {
				// Find current distance to ground
				// Needs to be upgraded to work with both types defined above.
				if(Physics.Raycast(vertexPoint.transform.position, -Vector3.up, out strikePoint, 200.0f, layerMask)) {
					projectedGroundPosition = strikePoint.point;
					currentDistance = strikePoint.distance;
					BMod_BalancePosition balPos = transform.parent.GetComponent<BMod_BalancePosition>(); // Hit and hope
					if (balPos!=null) {
						if(currentDistance!=distanceRequired) {
							Vector3 current = vertexPoint.transform.position;
							Vector3 shouldBe = new Vector3(current.x, distanceRequired, current.z);
							Vector3 moveTo = Vector3.MoveTowards(current, shouldBe, balPos.maxSlideMovementSpeed * Time.deltaTime);
							// Apply the translation only to the vertex
							vertexPoint.x += (moveTo.x - current.x);
							vertexPoint.y += (moveTo.y - current.y);
							vertexPoint.z += (moveTo.z - current.z);
						}
					}
				}
			}
		}

		public void updateProjectionValues() {
			// Currently only support ground only type
			RaycastHit strikePoint;
			int layerMask = 1 << 8;
			if(vertexPoint!=null) {
				if(Physics.Raycast(vertexPoint.transform.position, -Vector3.up, out strikePoint, 200.0f, layerMask)) {
					projectedGroundPosition = strikePoint.point;
					currentDistance = strikePoint.distance;
					//Debug.Log("Current distance height " + currentDistance + " for preferred " + distanceToHold);
					// For now using a built-in step height.
					preferredUnlockPosition = new Vector3(projectedGroundPosition.x, projectedGroundPosition.y+0.8f, projectedGroundPosition.z);
					//preferredUnlockPosition = -Vector3.up * (currentDistance * 0.5f); // Halfway to ground is unlock position
				}
			}
		}

	}


}