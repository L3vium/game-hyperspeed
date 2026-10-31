using System;
using UnityEngine;

namespace Obi
{
	/**
	 * Struct that holds data for a branch level: level, growth rate, etc.
	 */
	[Serializable]
	public class ObiPlantGenerator
	{

		public enum GeneratorType{
			Trunk,
			Branch,
			Leaves
		}

		[SerializeField][HideInInspector] public GeneratorType type = GeneratorType.Trunk;
		[SerializeField][HideInInspector] public string name = "Generator";
		[SerializeField][HideInInspector] public int parent = -1;
		[SerializeField][HideInInspector] public int id = 0;

		UnityEngine.Random.State rngState;
	

		public AnimationCurve shape = AnimationCurve.Constant(0,1,1);
		public float length = 5;
		public float thickness = 0.2f;
		public float resolution = 8;			/**< amount of particles per length unit.*/
		public float growthRate = 1;		

		[Range(0,1)]
		public float randomness = 0.1f;
		public Color color = Color.white;


		[Range(0,1)]
		public float phototropism = 0.1f;
		public float lightGrowthBoost = 1.5f;
		public float thigmotropism = 0.1f;


		public float massMultiplier = 1;
		public float rigidness = 10;


		public float frequency = 0.25f; 
		public int multiplicity = 1;

		[Range(0,1)]
		public float branchingProbability = 0.1f;
		public AnimationCurve branchingCurve = AnimationCurve.Constant(0,1,1);

		[Range(0,1)]
		public float spread = 0;
		[Range(0,360)]
		public float twirl = 0;
		[MinMaxAttribute(-1,1)]
		public Vector2 growthAngle = Vector2.zero;
		[MinMaxAttribute(0,1)]
		public Vector2 horizontalAlign = Vector2.zero;
		[Range(0,1)]
		public float verticalRandomness = 0.2f;
		[MinMaxAttribute(1,10)]
		public Vector2 distanceFromTrunk = Vector2.zero;

		public ObiPlant.LeafType leafType = ObiPlant.LeafType.Square;

		[Indent]
		//[VisibleIf("UsesCustomLeaves")]
		public Mesh customLeaf;


		public Vector3 leafScale = Vector3.one;

		[MinMaxAttribute(0,5)]
		public Vector2 sizeRange = new Vector2(1,2.5f); // scale range, use for branch length too (maybe curve?)
		[Range(0,1)]
		public float trunkScale = 0.5f;					// amount of size scaling by parent

		[Range(0,3)]
		public uint smoothness = 0;
		[Range(0,1)]
		public float curvatureDetail = 1;
		public float perimeterDetail = 40;
		public float thicknessScale = 1;
		public Vector2 uvScale = new Vector2(1,1);


		public ObiList<ObiPlant.Node> nodes = new ObiList<ObiPlant.Node>();		/**< List of nodes generated*/

		// Leaf generator data:

		public GeneratorType Type{	
			get{return type;}
		}

		public float MaxParticleLength{
			get{return 1 / Mathf.Max(0.1f,resolution);}
		}

		private bool UsesCustomLeaves(){
			return leafType == ObiPlant.LeafType.Custom;
		}

		public ObiPlantGenerator(GeneratorType type, string name, int id, int parent){
			this.type = type;
			this.name = name;
			this.id = id;
			this.parent = parent;
		}

		public void InitRNGState(int seed){
			UnityEngine.Random.InitState(seed);
			StoreRNGState();
		}

		public void StoreRNGState(){
			rngState = UnityEngine.Random.state;
		}

		public void LoadRNGState(){
			UnityEngine.Random.state = rngState;
		}

		/**
		 * Pregenerate nodes:
		 */
		public void GenerateNodes(ObiPlantGenerator parent){

			nodes = new ObiList<ObiPlant.Node>();

			if (parent.length > 0){
	
				float stepAngleIncrement = 360.0f / multiplicity;
				int amount = Mathf.CeilToInt(parent.length / frequency);
	
				// Loop over all curve sections:
				for (int i = 0; i < amount; ++i){
	
					float groupPos = frequency * i;
	
					if (UnityEngine.Random.value <= branchingProbability * branchingCurve.Evaluate(groupPos / parent.length)){
	
						for (int j = 0; j < multiplicity; ++j){
		
							float nodePos = (groupPos + frequency * (UnityEngine.Random.value-0.5f) * spread) / parent.length;
	
							if (nodePos >= 0 && nodePos < 1)
								nodes.Add(PlaceLocalNode(1,i*twirl + j*stepAngleIncrement,nodePos,id));
						}
					}
				}
			}
		}

		public ObiPlant.Node PlaceNode(ObiPathFrame section, Vector3 gravity, float angle, float normPos, int generatorId){

			// twirl the initial normal around the section's tangent:
			Vector3 normal = Quaternion.AngleAxis(angle,section.tangent) * section.normal;

			// Calculate node position:
            Vector3 nodePosition = section.position + normal * section.thickness * UnityEngine.Random.Range(distanceFromTrunk.x,distanceFromTrunk.y);

			// Calculate node normal and up vectors:
			Vector3 up = Vector3.Lerp(section.tangent,-gravity,UnityEngine.Random.Range(horizontalAlign.x,horizontalAlign.y));
			Vector3 bitangent = Vector3.Cross(normal,up);
			normal = Vector3.Cross(up,bitangent);
			
			// Calculate node orientation:
			Quaternion nodeGrowthAngle = Quaternion.AngleAxis(UnityEngine.Random.Range(growthAngle.x,growthAngle.y) * 90, bitangent);
			Quaternion nodeOrientation = nodeGrowthAngle * Quaternion.AngleAxis(UnityEngine.Random.Range(-90,90) * verticalRandomness,up) * Quaternion.LookRotation(normal,up);

			float leafSize = UnityEngine.Random.Range(sizeRange.x,sizeRange.y);
			
            return new ObiPlant.Node(nodePosition,nodeOrientation,leafScale * Mathf.Lerp(1,section.thickness,trunkScale) * leafSize, normPos, generatorId);
		}

		// returns a node in local space:
		public ObiPlant.Node PlaceLocalNode(float radius, float angle, float normPos, int generatorId){

			// twirl the initial normal around the section's tangent:
			Quaternion twirl = Quaternion.AngleAxis(angle,Vector3.right);
			Vector3 normal = twirl * Vector3.forward;

			// Calculate node position:
			Vector3 nodePosition = normal * radius * UnityEngine.Random.Range(distanceFromTrunk.x,distanceFromTrunk.y);
			
			// Calculate node orientation:
			Quaternion nodeGrowthAngle = Quaternion.AngleAxis(-90 + UnityEngine.Random.Range(growthAngle.x,growthAngle.y) * 90, Vector3.up);
			Quaternion nodeOrientation = twirl * Quaternion.AngleAxis(-90 + UnityEngine.Random.Range(-90,90) * verticalRandomness,Vector3.right) * nodeGrowthAngle * Quaternion.AngleAxis(90,Vector3.right);

			float leafSize = UnityEngine.Random.Range(sizeRange.x,sizeRange.y);
			
			return new ObiPlant.Node(nodePosition,nodeOrientation,leafScale * Mathf.Lerp(1,radius,trunkScale) * leafSize, normPos, generatorId);
		}
	
	}
}

