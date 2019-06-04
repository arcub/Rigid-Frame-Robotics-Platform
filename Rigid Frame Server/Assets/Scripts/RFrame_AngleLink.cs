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
			RFrame_Vertex vertexCentre = parentFrame.allVertices[this.centerVertex];
			RFrame_Vertex vertexUp = parentFrame.allVertices[this.upVertex];
			RFrame_Vertex vertexMeasure1 = parentFrame.allVertices[this.measure1];
			RFrame_Vertex vertexMeasure2 = parentFrame.allVertices[this.measure2];
			Vector3 centreVector = new Vector3(vertexCentre.x, vertexCentre.y, vertexCentre.z);
			Vector3 upVector = new Vector3(vertexUp.x, vertexUp.y, vertexUp.z);
			Vector3 measure1Vector = new Vector3(vertexMeasure1.x, vertexMeasure1.y, vertexMeasure1.z);
			Vector3 measure2Vector = new Vector3(vertexMeasure2.x, vertexMeasure2.y, vertexMeasure2.z);
			Vector3 measure1Calc = measure1Vector - centreVector;
			Vector3 measure2Calc = measure2Vector - centreVector;
			
			// Measure the angle based on the measuring angle type.
			switch (angleMeasureType) {
				case AngleMeasureTypeEnum.quaternionBased: {
					Vector3 upVertex = upVector - centreVector;
					Quaternion orientation1 = Quaternion.LookRotation(measure1Calc, upVertex);
					Quaternion orientation2 = Quaternion.LookRotation(measure2Calc, upVertex);
					angleBetween = Quaternion.Angle(orientation1, orientation2);
					break;
				}
				case AngleMeasureTypeEnum.lineBased: {
					// Use the dot product method of finding the angle
					float dotProduct = Vector3.Dot(measure1Calc, measure2Calc);
					float magMult = measure1Calc.magnitude * measure2Calc.magnitude;
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