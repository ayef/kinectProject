using UnityEngine;
using System.Collections;

public class FilterVisualization  {
	
	// AA: circle code from http://wiki.unity3d.com/index.php?title=TextureDrawCircle
	// Draws circle at cx, cy, on the passed texture
	public static void DrawCircle (Texture2D tex, int cx, int cy, int r, Color col) {

		int y = r;
		float d = 1/4 - r;
		float end = Mathf.Ceil(r/Mathf.Sqrt(2));
	 
		for (int x = 0; x <= end; x++) {
			tex.SetPixel(cx+x, cy+y, col);
			tex.SetPixel(cx+x, cy-y, col);
			tex.SetPixel(cx-x, cy+y, col);
			tex.SetPixel(cx-x, cy-y, col);
			tex.SetPixel(cx+y, cy+x, col);
			tex.SetPixel(cx-y, cy+x, col);
			tex.SetPixel(cx+y, cy-x, col);
			tex.SetPixel(cx-y, cy-x, col);
	 
			d += 2*x+1;
			if (d > 0) {
				d += 2 - 2*y--;
			}
		}
	}
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
