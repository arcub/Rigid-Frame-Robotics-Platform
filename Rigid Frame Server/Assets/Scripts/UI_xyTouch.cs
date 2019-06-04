using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_xyTouch : MonoBehaviour {

	private bool _mouseState = false;
	public Vector3 screenSpace;

	public Vector3 offset;

	public RectTransform dragShape;
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void OnMouseDown() {
			// Capture the offset of the cursor on this collider, but only if it hasn't already done so.
			if (!_mouseState) {
				_mouseState = true;
		        screenSpace = Camera.main.ScreenToViewportPoint(transform.localPosition);
		        offset = transform.localPosition - (new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f));
		Debug.Log("Mouse down on drag offset");
			}
		}

		void OnMouseUp() {
			// Return the trigger flag to false.
			_mouseState = false;
		}
		
		void OnMouseDrag() {
			Vector3 curScreenSpace = new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0.0f);
            //convert the screen mouse position to world point and adjust with offset
            Vector3 curPosition = curScreenSpace + offset;
            //update the position of the object in the world
            dragShape.transform.localPosition = curPosition;
		}

}
