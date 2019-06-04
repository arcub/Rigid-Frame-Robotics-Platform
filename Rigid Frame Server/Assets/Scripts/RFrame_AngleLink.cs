using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Version 2.0
// Andrew Forster 12 March 2019
// Added the option to use quaternions or the angle derived from the dot product.
// It still uses the two measurement vectors that the quaterions use.
// Andrew Forster 16 April 2019
// Added a scaling factor for the change in angle from initial measured. This
// should allow for slight compensation for sluggish response. But may lead to
// issues later on. Like "big swinging" movements maybe?

namespace RigidFrame_Development
{

	public class RFrame_AngleLink : MonoBehaviour {

		public enum AngleMeasureTypeEnum {
			quaternionBased,
			lineBased
		}
		public AngleMeasureTypeEnum angleMeasureType = AngleMeasureTypeEnum.quaternionBased;

		public float angleBetween = 0.0f;

		public float initialAngle = 0.0f;
		public float biasAngle = 90.0f;

		public bool invertedSendAngle = false;
		public float channelSendAngle = 0.0f;
		// The value to scale the change in angle by. This is still controlled by the min and max angle.
		public float angleChangeScaling = 1.0f;
		public float minimumAngle = 10.0f;
		public float maximumAngle = 160.0f;

		public int centerVertex = 0;
		public int upVertex = 0;
		public int measure1 = 0;
		public int measure2 = 0;
		public int linkChannel = 0;

		// Use this for initialization
		void Start () {
			// Attach the camera to the canvas
			
		}
		
		// Update is called once per frame
		void Update () {
			// Update this game objects position to be by the angle joint.
			RFrame_Object parentFrame = transform.parent.gameObject.GetComponent<RFrame_Object>();
			GameObject centerObj = parentFrame.allGameObjectVertices[this.centerVertex];
			transform.localPosition = centerObj.transform.localPosition;
			// Call the angle measuring function.
			measureAngle();
			
		}

		public void measureAngle(bool measuringInitialAngle = false) {
			RFrame_Object parentFrame = transform.parent.gameObject.GetComponent<RFrame_Object>();
			GameObject centerObj = parentFrame.allGameObjectVertices[this.centerVertex];
			GameObject upObj = parentFrame.allGameObjectVertices[this.upVertex];
			GameObject measure1Obj = parentFrame.allGameObjectVertices[this.measure1];
			GameObject measure2Obj = parentFrame.allGameObjectVertices[this.measure2];
			Vector3 measure1Vertex = measure1Obj.transform.localPosition - centerObj.transform.localPosition;
			Vector3 measure2Vertex = measure2Obj.transform.localPosition - centerObj.transform.localPosition;
			
			// Measure the angle based on the measuring angle type.
			switch (angleMeasureType) {
				case AngleMeasureTypeEnum.quaternionBased: {
					Vector3 upVertex = upObj.transform.localPosition - centerObj.transform.localPosition;
					Quaternion orientation1 = Quaternion.LookRotation(measure1Vertex, upVertex);
					Quaternion orientation2 = Quaternion.LookRotation(measure2Vertex, upVertex);
					angleBetween = Quaternion.Angle(orientation1, orientation2);
					break;
				}
				case AngleMeasureTypeEnum.lineBased: {
					// Use the dot product method of finding the angle
					float dotProduct = Vector3.Dot(measure1Vertex, measure2Vertex);
					float magMult = measure1Vertex.magnitude * measure2Vertex.magnitude;
					if(magMult>0.0f) {
						// Converted to degrees.
						angleBetween = Mathf.Acos(dotProduct / magMult) * Mathf.Rad2Deg;
					}
					break;
				}
			}

			// Record the initial angle
			if (measuringInitialAngle) {
				initialAngle = angleBetween;
				
			}
			// Calculate the change in angle and apply that to the bias angle to be sent to the channel
			float diff = (angleBetween - initialAngle);
			float scaledDiff = diff * angleChangeScaling;
			channelSendAngle = biasAngle + (invertedSendAngle ? -scaledDiff : scaledDiff);
			// Check if channel angle is within limits.
			if (channelSendAngle < minimumAngle) {
				channelSendAngle = minimumAngle;
			}
			if (channelSendAngle > maximumAngle) {
				channelSendAngle = maximumAngle;
			}
		}

	}

}