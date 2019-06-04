using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RigidFrame_Development
{
	public class RFrame_Vertex : MonoBehaviour {

		float _x;
		float _y;
		float _z;
		public float x {
			get { return _x;}
			set { _x = value;}
		}
		public float y {
			get { return _y; }
			set { _y = value;}
		}
		public float z {
			get { return _z;}
			set { _z = value;}
		}
		float effectorCount = 0;
		float xWork;
		float yWork;
		float zWork;
		bool workAlignment;
		public bool lockedInPlace = false;
		public VertexType vertexType;

		//public Vector3 offset;

			// Use this for initialization
		//RFrame_Bridge simulator;
		void Start () {
			//simulator = transform.parent.parent.GetComponent<RFrame_Bridge>();
		}

		/**
		* The different types of vertices that I can think of so far.
		* Bound to change at some point.
		*/
		public enum VertexType {
			/**
			* This type of vertex functions as a pure vertex. No additional roles.
			*/
			pure,
			/**
			* This vertex is used as the point from which to measure the group
			* radius.
			*/
			groupCenter,
			/**
			* Used in goal achievement role calls. Each focal vertex will have
			* a bias ID, with a preference for lower numbers in the goal achiever
			* builder class (Do be designed in next iteration)
			*/
			focal
			
		}

		/**
		* Supply a vertex to return the distance to that vertex from this one.
		* @param toVertex The vertex to measure the distance too.
		* @param normalise Indicates that the difference values are to be normalised.
		* @return A four item array, containing the distance and the x y z difference between the vertices. Normalised if told too.
		*/
		public float[] distanceToVertex(RFrame_Vertex toVertex, bool normalise)
		{
			float diffX = toVertex.x - this.x;
			float diffY = toVertex.y - this.y;
			float diffZ = toVertex.z - this.z;
			float dist = Mathf.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);
			float[] returnVals = {dist, diffX, diffY, diffZ};
			if (normalise) {
				returnVals[1] = diffX / dist;
				returnVals[2] = diffY / dist;
				returnVals[3] = diffZ / dist;
			}
			return returnVals;
		}

		/**
		* When the work alignment switch of this vertex does not match the
		* work alignment of the current cycle, the work variables are reset
		* by applying the mov variables directly and then setting this
		* vertex's work alignment to match.
		* @param xmov Translation on the x-axis
		* @param ymov Translation on the y-axis
		* @param zmov Translation on the z-axis
		* @param affirmer This allows work alignment switching synchronisation
		*/
		public void applyTranslation(float xmov, float ymov, float zmov, RFrame_Sim affirmer)
		{
			
			if (workAlignment != affirmer.getWorkAlignment()) {
				workAlignment = affirmer.getWorkAlignment();
				xWork = xmov;
				yWork = ymov;
				zWork = zmov;
				effectorCount = 1;
			} else {
				xWork += xmov;
				yWork += ymov;
				zWork += zmov;
				effectorCount++;
			}
		}
		
		/**
		* This is designed as a test to see if the finaliseTranslations function can
		* be merged into the apply translation function so that when the work alignment
		* changes then the previous tranlation and effector count are applied.
		* This should allow the removal of the looping through all the vertices at the
		* end of a cycle.
		 */
		public void applyTranslationV2(float xmov, float ymov, float zmov, RFrame_Sim affirmer)
		{
			// Check if the work alignment matches.
			if (workAlignment != affirmer.getWorkAlignment()) {
				// The finalise translations is placed here before
				// resetting.
				if (effectorCount > 0) {
					x += (xWork / effectorCount);
					y += (yWork / effectorCount);
					z += (zWork / effectorCount);
					Transform tr = GetComponent<Transform>();
					tr.localPosition = new Vector3(x,y,z);
				}
				workAlignment = affirmer.getWorkAlignment();
				xWork = xmov;
				yWork = ymov;
				zWork = zmov;
				effectorCount = 1;
			} else {
				xWork += xmov;
				yWork += ymov;
				zWork += zmov;
				effectorCount++;
			}
		}

		/**
		* Applies a translation that isn't effected by an Affirmer. The rate of
		* movement will be controlled by the effector/environment.
		* This essentially works like finaliseTranslations, but without the
		* hassle of tracking the workAlignment.
		* @param xmov
		* @param ymov
		* @param zmov 
		*/
		public void applyEffectTranslation(float xmov, float ymov, float zmov) {
			// Nothing fancy, just apply the translation.
			x += xmov;
			y += ymov;
			z += zmov;
		}
		
		/**
		* All translations are applied to vertices that have a effectorCount
		* of greater than zero. If they don't then their work alignment is
		* boolean waltzed.
		* @param affirmer
		*/
		public void finaliseTranslations(RFrame_Sim affirmer)
		{
			if (effectorCount > 0) {
				x += (xWork / effectorCount);
				y += (yWork / effectorCount);
				z += (zWork / effectorCount);
				Transform tr = GetComponent<Transform>();
				tr.localPosition = new Vector3(x,y,z);
			} else {
				// This is done so that this vertex doesn't get out of step
				// with the other vertices. Especially since I'm using a
				// boolean waltz. :)
				workAlignment = affirmer.getWorkAlignment();
			}
		}
	}

}
