using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obi{

	public partial class ObiPlant : ObiActor 
	{

		private const float MAX_CURV_COS = 0.939f;   /**< Took 20 degrees as the maximum pruneable curvature.*/
		private const float ONE_MINUS_MAX_CURV_COS = 1 - MAX_CURV_COS;

		public enum LeafType{
			Square,
			Cross,
			Diamond,
			DiamondCross,
			Long,
			Custom 
		}
 
		private ObiList<ObiPathFrame> controlPoints = new ObiList<ObiPathFrame>();
		private ObiList<Node> leafNodes = new ObiList<Node>();

		private List<Vector3> vertices = new List<Vector3>();
		private List<Vector3> normals = new List<Vector3>();
		private List<Vector4> tangents = new List<Vector4>();
		private List<Vector2> uvs = new List<Vector2>();
		private List<Color> vertColors = new List<Color>();
		private List<int> trunkTriangles = new List<int>();

		private Mesh trunkMesh;
		private Mesh plantMesh;
	
        protected override void OnEnable(){
			base.OnEnable();

			LazyMeshInitialization();

			MeshFilter filter = GetComponent<MeshFilter>();
			filter.mesh = plantMesh;

		}

        protected override void OnDisable(){
			base.OnDisable();

			DestroyImmediate(plantMesh);
			DestroyImmediate(trunkMesh);
		}

		protected void LazyMeshInitialization(){

			if (trunkMesh == null){
				trunkMesh = new Mesh();
				trunkMesh.name = "trunkMesh";
				trunkMesh.MarkDynamic();
			}

			if (plantMesh == null){
				plantMesh = new Mesh();
				plantMesh.name = "plantMesh";
				plantMesh.MarkDynamic();
			}
		}
	
		protected void UpdateMesh () {

			LazyMeshInitialization();

			plantMesh.Clear();

			if (solver == null)
				return;

			GenerateTrunkGeometry();

			GenerateLeafGeometry();

		}

        private void AppendSectionVertices(ObiPathFrame frame, float angle, float radius, Color color, Vector2 uv){

            vertices.Add(frame.position + (Mathf.Cos(angle) * frame.normal + Mathf.Sin(angle) * frame.binormal) * radius);
            normals.Add(vertices[vertices.Count-1] - frame.position);
			Vector4 texTangent = Vector3.Cross(normals[normals.Count-1],frame.tangent);
			texTangent.w = 1;
			tangents.Add(texTangent);

			vertColors.Add(color);
			uvs.Add(uv);
		}	

		private int SegmentCountFromRadius(float radius, float perimeterDetail){
			return Mathf.Max(3,(int)(radius * perimeterDetail));
		}

		/**
		 * Clears the plant mesh, and then fills it with trunk geometry by first calculating curves and then generating vertices/triangles
		 * around them. 
		 */
		public void GenerateTrunkGeometry(){

			ClearMeshData(); 
			GenerateTrunkCurves();

			int vertexCount = 0;
			Vector2 uv = Vector2.zero;

            ObiPathFrame frame = new ObiPathFrame();

			// Loop over all branch curves:
			for (int m = 0; m < meristems.Count; ++m){
				
				Meristem meristem = meristems[m];
				ObiPlantGenerator gen = generators[meristem.generatorId];

				// Reset the curve frame and texture coordinate:
				frame.Reset();
				float vCoord = 0;

				// Loop over all curve sections:
				for (int i = 0; i < meristem.curve.Count; ++i){

                    int sectionSegments = SegmentCountFromRadius(meristem.curve[i].thickness,gen.perimeterDetail);
					int sectionVertices = sectionSegments+1;

					// Append vertices
					frame = meristem.curve[i];
					float angle = 2 * Mathf.PI / sectionSegments;
					for (int j = 0; j <= sectionSegments; ++j){

						uv.Set((j/(float)sectionSegments)*gen.uvScale.x,vCoord);
                        AppendSectionVertices(frame,angle*j,meristem.curve[i].thickness,meristem.curve[i].color,uv);

					}

					// Add section triangles:
					if (i < meristem.curve.Count-1){

						int sectionCount = 0;
						int nextSectionCount = 0;
                        int nextSectionSegments = SegmentCountFromRadius(meristem.curve[i+1].thickness,gen.perimeterDetail);
	
						// Calculate target ratio:
						float targetRatio = nextSectionSegments/(float)sectionSegments;

						// Keep adding triangles until both the current section and the next section vertices are all referenced.
						while(sectionCount < sectionSegments || nextSectionCount < nextSectionSegments){
	
							float ratio = nextSectionCount/(float)sectionCount;

							// Try to balance out the next/current section ratio when adding triangles, for good topology.
							if (ratio > targetRatio && sectionCount < sectionSegments){

								trunkTriangles.Add(vertexCount + sectionCount);
								trunkTriangles.Add(vertexCount + sectionCount+1);
								trunkTriangles.Add(vertexCount + sectionVertices + nextSectionCount);

								sectionCount++;

							}else{

								trunkTriangles.Add(vertexCount + sectionCount);
								trunkTriangles.Add(vertexCount + sectionVertices + nextSectionCount+1);
								trunkTriangles.Add(vertexCount + sectionVertices + nextSectionCount);

								nextSectionCount++;
							}
						}
					}

					vertexCount += sectionVertices;
					vCoord += gen.uvScale.y;

				}

			}

			CommitMeshData();
		}

		private bool AddCurveSection(Matrix4x4 w2local, Quaternion w2localRot, int particleIndex, int parentIndex, float curvatureDetail, float thicknessScale){

			// Check if we have arrived at the end of the curve:
			bool root = parentIndex < 0 || levels[parentIndex] != levels[particleIndex];

			Vector3 localRight = w2localRot * solver.orientations[solverIndices[particleIndex]] * Vector3.right;
			Vector3 position = w2local.MultiplyPoint3x4(solver.positions[solverIndices[particleIndex]]); 
	
			Vector3 tangent = localRight;
			if (parentIndex > 0)
				tangent = (position - w2local.MultiplyPoint3x4(solver.positions[solverIndices[parentIndex]])).normalized;

			// Calculate curvature.
			float curvature = Mathf.Max(0,Vector3.Dot(tangent,localRight) - MAX_CURV_COS) / ONE_MINUS_MAX_CURV_COS;

			// Do not skip section if it is too curvy, or if it's the root section:
			if (curvature <= curvatureDetail + 0.0001f || root){
		
				Vector3 localUp = w2localRot * solver.orientations[solverIndices[particleIndex]] * Vector3.up;
				Vector3 normal = Vector3.Cross(tangent,localUp).normalized;

				// calculate section thickness (either constant, or particle radius based):
				//float sectionThickness = (parentIndex > 0 ? principalRadii[parentIndex][2] : principalRadii[particleIndex][2]) * thicknessScale;

				//Vector3 midPoint = w2local.MultiplyPoint3x4((Vector3)Solver.positions[particleIndices[particleIndex]] - Solver.orientations[particleIndices[particleIndex]] * Vector3.right * principalRadii[particleIndex][0]);

				//controlPoints.Add(new ObiCurveSection(new Vector4(midPoint.x,midPoint.y,midPoint.z,sectionThickness),tangent,normal,Color.white));
			}

			return root;
		}

		public void GenerateTrunkCurves(){

			Matrix4x4 w2local = transform.worldToLocalMatrix;
			Quaternion w2localRot = w2local.rotation;

			for (int m = 0; m < meristems.Count; ++m){

				ObiPlant.Meristem meristem = meristems[m];
				ObiPlantGenerator gen = generators[meristem.generatorId];

				if (meristem.curve == null)
					meristem.curve = new ObiList<ObiPathFrame>();
				else meristem.curve.Clear();

				controlPoints.Clear();

				int particleIndex = meristem.particleIndex;
				int parentIndex = parentIndices[particleIndex];


                float sectionThickness = 0;//principalRadii[particleIndex][2] * gen.thicknessScale;

				Quaternion particleOrientation = base.solver.orientations[solverIndices[particleIndex]];
				Vector3 particlePosition = base.solver.positions[solverIndices[particleIndex]];

                Vector3 position = Vector3.zero;//w2local.MultiplyPoint3x4(particlePosition + particleOrientation * Vector3.right * principalRadii[particleIndex][0]); 
				Vector3 tangent = w2localRot * particleOrientation * Vector3.right;
				tangent.Normalize();
			
				Vector3 normal = Vector3.Cross(tangent,w2localRot * particleOrientation * Vector3.up).normalized;

				//controlPoints.Add(new ObiCurveSection(new Vector4(position.x,position.y,position.z,sectionThickness),
				//										   		  tangent,normal,Color.white));

				while(true){

					// Stop if arrived to the root:
					if (AddCurveSection(w2local,w2localRot,particleIndex,parentIndex, gen.curvatureDetail, gen.thicknessScale)) 
						break;

					// Update particle and parent indices: 
					particleIndex = parentIndex;
					parentIndex = parentIndices[particleIndex];

				}

				//ObiCurveFrame.Chaikin(controlPoints,meristem.curve,gen.smoothness);
				
			}
	
		}

		private void GenerateLeafGeometry(){

			GenerateLeaves();

			Mesh leavesMesh = new Mesh();

			Matrix4x4 l2World = transform.localToWorldMatrix;
			Quaternion l2WorldRot = l2World.rotation;

			CombineInstance[] leaves = new CombineInstance[leafNodes.Count];

			for (int i = 0; i < leafNodes.Count; ++i){
				
				//Debug.DrawRay(l2World.MultiplyPoint3x4(leafNodes[i].position),l2WorldRot * leafNodes[i].orientation * Vector3.forward * 0.5f,Color.yellow);

				ObiPlantGenerator gen = generators[leafNodes[i].generatorId];

				Mesh leafMesh = gen.customLeaf;
				if (gen.leafType != LeafType.Custom)
					leafMesh = BuiltInLeafShapes.GetLeafMesh(gen.leafType);

				if (leafMesh != null){

					CombineInstance instance = new CombineInstance();
					instance.mesh = leafMesh;
					instance.subMeshIndex = 0;
					instance.transform = leafNodes[i].GetTransform();

					leaves[i] = instance;
				}

			}

			leavesMesh.CombineMeshes(leaves,true,true);

			CombineInstance leavesInstance = new CombineInstance();
			leavesInstance.mesh = leavesMesh;

			CombineInstance trunkInstance = new CombineInstance();
			trunkInstance.mesh = trunkMesh;
			
			plantMesh.CombineMeshes(new CombineInstance[]{trunkInstance,leavesInstance},false,false,false);
		}

		private ObiPathFrame GetCurveSectionAtNormalizedValue(float value, ObiList<ObiPathFrame> curve, float length){

			if (curve.Count == 0)
				return ObiPathFrame.Identity;

            float normalizedLength = length * value;
			float accumLen = 0;

			for (int i = curve.Count-1; i > 0; --i){
				float d = Vector3.Distance(curve[i].position,curve[i-1].position);
				if (accumLen + d >= normalizedLength){
					float mu = (normalizedLength - accumLen) / d;
					return (1-mu)*curve[i] + mu*curve[i-1];
				}else{
					accumLen += d;
				}
			}

			return curve[0];
		}		

		private void GenerateLeaves(){

			// First, clear all existing leaf nodes.
			leafNodes.Clear();
			
			// Iterate over each meristem:
			for (int m = 0; m < meristems.Count; ++m){

				// For each meristem, iterate over leaf generators:
				for (int i = 0; i < generators.Count; ++i){

					ObiPlantGenerator gen = generators[i];
					int parentId = gen.parent >= 0 ? generators[gen.parent].id : -1;
					
					switch(gen.Type){
						case ObiPlantGenerator.GeneratorType.Leaves: {
	
							// Only grow leaves in the parent meristem:
							if (meristems[m].generatorId == parentId)
								GenerateLeafNodes(gen, meristems[m]);
	
						}break;
				
					}
				}

			}
			
		}

		private void GenerateLeafNodes(ObiPlantGenerator gen, ObiPlant.Meristem meristem){

			if (Mathf.Approximately(gen.frequency,0))
				return;

			Vector3 worldDown = transform.worldToLocalMatrix.MultiplyVector(Vector3.down); // TODO: use solver gravity.

			UnityEngine.Random.State state = UnityEngine.Random.state;

			UnityEngine.Random.InitState(0);

			float stepAngleIncrement = 360.0f / gen.multiplicity;

			int amount = Mathf.CeilToInt(meristem.length / gen.frequency);

			// Loop over all curve sections:
			for (int i = 0; i < amount; ++i){

				float groupPos = gen.frequency * i;

				for (int j = 0; j < gen.multiplicity; ++j){

					float normValue = (groupPos + gen.frequency * (Random.value-0.5f) * gen.spread) / meristem.length;
					ObiPathFrame section = GetCurveSectionAtNormalizedValue(normValue,meristem.curve,meristem.length);
					leafNodes.Add(gen.PlaceNode(section,worldDown,i*gen.twirl + j*stepAngleIncrement,normValue,gen.id));
	
				}
			}

			UnityEngine.Random.state = state;
		}

		private void ClearMeshData(){
			trunkMesh.Clear();
			vertices.Clear();
			normals.Clear();
			tangents.Clear(); 
			uvs.Clear();
			vertColors.Clear();
			trunkTriangles.Clear();
		}

		private void CommitMeshData(){
			trunkMesh.SetVertices(vertices);
			trunkMesh.SetNormals(normals);
			trunkMesh.SetTangents(tangents);
			trunkMesh.SetColors(vertColors);
			trunkMesh.SetUVs(0,uvs);
			trunkMesh.SetTriangles(trunkTriangles,0,true);
		}
	}
}
