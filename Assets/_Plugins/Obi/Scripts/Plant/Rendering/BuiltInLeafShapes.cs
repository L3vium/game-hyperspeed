using System;
using UnityEngine;

namespace Obi
{
	public static class BuiltInLeafShapes
	{
		private static Mesh squareLeaf;
		private static Mesh crossLeaf;
		private static Mesh diamondLeaf;
		private static Mesh diamondCrossLeaf;

		public static Mesh GetLeafMesh(ObiPlant.LeafType type){
			switch(type){
				case ObiPlant.LeafType.Square: return GetSquareLeaf();
				case ObiPlant.LeafType.Cross: return GetCrossLeaf();
				case ObiPlant.LeafType.Diamond: return GetDiamondLeaf();
				case ObiPlant.LeafType.DiamondCross: return GetDiamondCrossLeaf();
				//case ObiPlantRenderer.LeafType.Long: return GetLongLeaf();
				default: return null;
			}
		}		

		private static Mesh GetSquareLeaf()
		{
			if (squareLeaf == null){
				squareLeaf = new Mesh();
			
				squareLeaf.vertices = new Vector3[]{
					new Vector3(-0.5f,0,0),
					new Vector3(0.5f,0,0),
					new Vector3(0.5f,0,1),
					new Vector3(-0.5f,0,1)
				};
	
				squareLeaf.uv = new Vector2[]{
					new Vector2(0,0),
					new Vector2(1,0),
					new Vector2(1,1),
					new Vector2(0,1)
				};
	
				squareLeaf.normals = new Vector3[]{
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up
				};
	
				squareLeaf.tangents = new Vector4[]{
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1)
				};
	
				squareLeaf.triangles = new int[]{
					0,2,1,
					0,3,2
				};
			}
			return squareLeaf;
		}

		private static Mesh GetCrossLeaf()
		{
			if (crossLeaf == null){
				crossLeaf = new Mesh();
			
				crossLeaf.vertices = new Vector3[]{
					new Vector3(-0.5f,0,0),
					new Vector3(0.5f,0,0),
					new Vector3(0.5f,0,1),
					new Vector3(-0.5f,0,1),
					new Vector3(0,-0.5f,0),
					new Vector3(0,0.5f,0),
					new Vector3(0,0.5f,1),
					new Vector3(0,-0.5f,1)
				};
	
				crossLeaf.uv = new Vector2[]{
					new Vector2(0,0),
					new Vector2(1,0),
					new Vector2(1,1),
					new Vector2(0,1),
					new Vector2(0,0),
					new Vector2(1,0),
					new Vector2(1,1),
					new Vector2(0,1)
				};
	
				crossLeaf.normals = new Vector3[]{
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.right,
					Vector3.right,
					Vector3.right,
					Vector3.right
				};
	
				crossLeaf.tangents = new Vector4[]{
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1)
				};
	
				crossLeaf.triangles = new int[]{
					0,2,1,
					0,3,2,
					6,4,5,
					7,4,6
				};
			}
			return crossLeaf;
		}

		private static Mesh GetDiamondLeaf()
		{
			if (diamondLeaf == null){
				diamondLeaf = new Mesh();
			
				diamondLeaf.vertices = new Vector3[]{
					new Vector3(0,0,0),
					new Vector3(-0.5f,0,0.5f),
					new Vector3(0,0.2f,0.5f),
					new Vector3(0,0,1),
					new Vector3(0.5f,0,0.5f)
				};
	
				diamondLeaf.uv = new Vector2[]{
					new Vector2(0.5f,0),
					new Vector2(0,0.5f),
					new Vector2(0.5f,0.5f),
					new Vector2(0.5f,1),
					new Vector2(1,0.5f)
				};
	
				diamondLeaf.normals = new Vector3[]{
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up
				};
	
				diamondLeaf.tangents = new Vector4[]{
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1)
				};
	
				diamondLeaf.triangles = new int[]{
					0,1,2,
					1,3,2,
					2,3,4,
					0,2,4
				};
			}
			return diamondLeaf;
		}

		private static Mesh GetDiamondCrossLeaf()
		{
			if (diamondCrossLeaf == null){
				diamondCrossLeaf = new Mesh();
			
				diamondCrossLeaf.vertices = new Vector3[]{
					new Vector3(0,0,0),
					new Vector3(0.5f,0,0.5f),
					new Vector3(0,0,1),
					new Vector3(-0.5f,0,0.5f),

					new Vector3(0,0,0),
					new Vector3(0,0.5f,0.5f),
					new Vector3(0,0,1),
					new Vector3(0,-0.5f,0.5f)
				};
	
				diamondCrossLeaf.uv = new Vector2[]{
					new Vector2(0.5f,0),
					new Vector2(1,0.5f),
					new Vector2(0.5f,1),
					new Vector2(0,0.5f),

					new Vector2(0.5f,0),
					new Vector2(1,0.5f),
					new Vector2(0.5f,1),
					new Vector2(0,0.5f)
				};
	
				diamondCrossLeaf.normals = new Vector3[]{
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.up,
					Vector3.right,
					Vector3.right,
					Vector3.right,
					Vector3.right
				};
	
				diamondCrossLeaf.tangents = new Vector4[]{
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(1,0,0,-1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1),
					new Vector4(0,1,0,1)
				};
	
				diamondCrossLeaf.triangles = new int[]{
					0,2,1,
					0,3,2,
					6,4,5,
					7,4,6
				};
			}
			return diamondCrossLeaf;
		}


	}
}

