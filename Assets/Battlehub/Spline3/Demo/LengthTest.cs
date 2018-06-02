using UnityEngine;
using Battlehub.Spline3;

[ExecuteInEditMode]
public class LengthTest : MonoBehaviour {

    public SplineBase Spline;

	// Use this for initialization
	void Start () {
	    	
	}
	
	// Update is called once per frame
	void Update () {
        Debug.Log(Spline.GetLength(0.01f) + " " + Spline.GetLength(0.01f, 0.0f, 0.5f));
	}
}
