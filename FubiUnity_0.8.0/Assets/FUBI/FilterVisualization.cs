using UnityEngine;
using System.Collections;

// AA: This class handles to all the visualizations for the filter
public class FilterVisualization  {
	
	public Texture2D m_filterOutputTexture;	// Texture used for drawing the filter outputs
	public int [] filterOutputIndices;		// Used in case of multiple filters
	public int filterOutputLocX = Screen.width/3;	// Coordinates of top left corner of filter output texture
	public int filterOutputLocY = 10;					// 
	public int filterOutputWidth;	// Width and height of the kinect-writeable area
	public int filterOutputHeight;	// Initialized in Start()
	public int borderWidth = 10;	// Control witdh of border on all sides on right & bottom side of writeable area
	
	private int numPens = 5;		// Number of pens for drawing filters with (should be dynamic
	
	Color [][] pens;				// Colors for colouring the different filter outputs

	public void SetPixels(Vector2 absPosition) 
	{
		m_filterOutputTexture.SetPixels((int)( absPosition.x), (int)((filterOutputHeight - absPosition.y) ), 2, 2, pens[1] );
		
	}
	
	public FilterVisualization () 
	{
		Color[] colors = new Color[numPens];
		colors[0] = Color.blue;
		colors[1] = Color.red;
		colors[2] = Color.green;
		colors[3] = Color.magenta;
		colors[4] = Color.black;
		
		pens = new Color[numPens][];
		
		for (int i = 0; i < numPens; i++) {
			pens[i] = new Color[4] ;
			pens[i][0] = pens[i][1] = pens[i][2] = pens[i][3] = colors[i];
		}
	}

	//AA: 
	public void Initialise() 
	{
		filterOutputWidth = Screen.width - filterOutputLocX - borderWidth ;
		filterOutputHeight = Screen.height - filterOutputLocY - borderWidth;
		
		if( m_filterOutputTexture == null)
			m_filterOutputTexture = new Texture2D(filterOutputWidth , filterOutputHeight);
		else
			m_filterOutputTexture.Resize (filterOutputWidth , filterOutputHeight);
	}
	
	public void DrawCircle()
	{
		DrawCircle(m_filterOutputTexture, m_filterOutputTexture.width/2, m_filterOutputTexture.height/2, (int)(m_filterOutputTexture.width*0.25f), Color.yellow);
		Debug.Log("Circle radius: " +(m_filterOutputTexture.height/m_filterOutputTexture.width)*100 );
	}
	
	// AA: circle code from http://wiki.unity3d.com/index.php?title=TextureDrawCircle
	// Draws circle at cx, cy, on the passed texture
	private static void DrawCircle (Texture2D tex, int cx, int cy, int r, Color col) {

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
	
	
	// AA: Code for drawing lines from: http://wiki.unity3d.com/index.php?title=DrawLine
	// http://wiki.unity3d.com/index.php?title=TextureDrawLine
	private void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color col)
	{
	 	int dy = (int)(y1-y0);
		int dx = (int)(x1-x0);
	 	int stepx, stepy;
	 
		if (dy < 0) {dy = -dy; stepy = -1;}
		else {stepy = 1;}
		if (dx < 0) {dx = -dx; stepx = -1;}
		else {stepx = 1;}
		dy <<= 1;
		dx <<= 1;
	 
		float fraction = 0;
	 
		tex.SetPixel(x0, y0, col);
		if (dx > dy) {
			fraction = dy - (dx >> 1);
			while (Mathf.Abs(x0 - x1) > 1) {
				if (fraction >= 0) {
					y0 += stepy;
					fraction -= dx;
				}
				x0 += stepx;
				fraction += dy;
				tex.SetPixel(x0, y0, col);
			}
		}
		else {
			fraction = dx - (dy >> 1);
			while (Mathf.Abs(y0 - y1) > 1) {
				if (fraction >= 0) {
					x0 += stepx;
					fraction -= dy;
				}
				y0 += stepy;
				fraction += dx;
				tex.SetPixel(x0, y0, col);
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
