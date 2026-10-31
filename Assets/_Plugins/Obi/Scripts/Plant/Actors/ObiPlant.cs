using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Obi
{
    [ExecuteInEditMode]
    //[AddComponentMenu("Physics/Obi/Obi Plant")]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    //[RequireComponent(typeof (ObiStretchShearConstraints))]
    //[RequireComponent(typeof (ObiBendTwistConstraints))]
    [DisallowMultipleComponent]
    public partial class ObiPlant : ObiActor
    {
        public const float DEFAULT_PARTICLE_ROTATIONAL_MASS = 0.005f;
        public const float MAX_YOUNG_MODULUS = 25000000000;
        public const float MIN_YOUNG_MODULUS = 10;

        [Serializable]
        public class Meristem
        {

            [Flags]
            public enum GrowthType
            {
                None = 0,
                Growing = 1 << 0,
                Tip = 1 << 1
            }

            public int particleIndex;                   /**< Index of the meristemic particle.*/
            public int generatorId;                     /**< Id of the generator that spawned this meristem.*/
            public float availableGrowthTime;           /**< Amount of time available for this meristem to grow.*/
            public float growthBoost;
            public GrowthType growth;                   /**< Whether this meristem is growing or not.*/
            public float length;                        /**< Length of the branch that ends at this meristem.*/
            public float thickness;                     /**< Base meristem thickness.*/
            public ObiList<ObiPathFrame> curve;     /**< List of sections along this meristem's branch.*/

            public Meristem(int particleIndex, int generatorId, GrowthType growth, float length, float thickness, float availableGrowthTime)
            {
                this.generatorId = generatorId;
                this.particleIndex = particleIndex;
                this.availableGrowthTime = availableGrowthTime;
                this.growthBoost = 1;
                this.growth = growth;
                this.length = length;
                this.thickness = thickness;
                this.curve = new ObiList<ObiPathFrame>();
            }
        }

        public struct Node
        {
            public Vector3 position;
            public Quaternion orientation;
            public Vector3 scale;
            public float normPos;   /**< position of the node along its parent branch.*/
            public int generatorId;

            public Node(Vector3 position, Quaternion orientation, Vector3 scale, float normPos, int generatorId)
            {
                this.position = position;
                this.orientation = orientation;
                this.scale = scale;
                this.normPos = normPos;
                this.generatorId = generatorId;
            }

            public Matrix4x4 GetTransform()
            {
                return Matrix4x4.TRS(position, orientation, scale);
            }
        }

        public class SurfacePoint
        {

            public float distance = float.MaxValue;
            public Vector3 direction = Vector3.zero;

        }

        public int pooledParticles = 250;
        public bool instantGrowth = true;
        public int seed = 0;

        public List<ObiPlantGenerator> generators = new List<ObiPlantGenerator>();

        [SerializeField] [HideInInspector] private int usedParticles = 0;

        [SerializeField] [HideInInspector] public List<Meristem> meristems;
        [SerializeField] [HideInInspector] public int[] parentIndices;
        [SerializeField] [HideInInspector] public float[] parentOffsets;    /**<normalized offset from the parent particle's center*/
        [SerializeField] [HideInInspector] public int[] levels;
        public SurfacePoint[] closestSurfaces;

        /*public ObiBendTwistConstraints BendTwistConstraints{
			get{return GetConstraintsByType(Oni.ConstraintType.BendTwist) as ObiBendTwistConstraints;}
		}
		public ObiStretchShearConstraints StretchShearConstraints{
			get{return GetConstraintsByType(Oni.ConstraintType.StretchShear) as ObiStretchShearConstraints;}
		}	*/

        public int UsedParticles
        {
            get { return usedParticles; }
        }

        public override ObiActorBlueprint sourceBlueprint
        {
            get { return null; }
        }

        /*public void OnDrawGizmos(){

			if (!InSolver) return;

			//Gizmos.matrix = transform.localToWorldMatrix;

			for (int i = 0; i < usedParticles; ++i){
				int s = particleIndices[i];
				Gizmos.color = Color.red;
				//Gizmos.DrawWireSphere(solver.positions[s],0.05f);
				
				Gizmos.DrawRay(solver.positions[s],solver.orientations[s] * Vector3.right*0.2f);
				Gizmos.color = Color.green;
				Gizmos.DrawRay(solver.positions[s],solver.orientations[s] * Vector3.up*0.2f);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(solver.positions[s],solver.orientations[s] * Vector3.forward*0.2f);


				/*if (i > 0){
					ObiStretchShearConstraintBatch stretchBatch = StretchShearConstraints.GetFirstBatch();
					Gizmos.color = Color.yellow;
					Gizmos.DrawRay(solver.positions[i-1], (solver.orientations[i-1] * stretchBatch.restOrientations [i-1]) * Vector3.forward * stretchBatch.restLengths [i - 1]);
				}*/
        /*}
    }*/

        public void Start()
        {
            GenerateNodes();

            //if (!instantGrowth && Application.isPlaying)
            //ResetActor();
        }

        private void GenerateNodes()
        {

            for (int i = 0; i < generators.Count; ++i)
            {

                ObiPlantGenerator gen = generators[i];
                gen.InitRNGState(seed);

                if (gen.parent >= 0)
                {
                    gen.GenerateNodes(generators[gen.parent]);
                    gen.StoreRNGState();
                }
            }

        }

        /*public override bool AddToSolver(){

			closestSurfaces = new SurfacePoint[pooledParticles];
			for (int i = 0; i < closestSurfaces.Length; ++i)
				closestSurfaces[i] = new SurfacePoint();
			
			if (solver != null)
				solver.OnCollision += Solver_OnCollision;
			return base.AddToSolver();
		}
		
		public override bool RemoveFromSolver(){

			if (solver != null)
				solver.OnCollision -= Solver_OnCollision;
			return base.RemoveFromSolver();
		}*/

        /**
	 	* Generates the particle based physical representation of the rope. This is the initialization method for the rope object
		* and should not be called directly once the object has been created.
	 	*/
        /*protected override IEnumerator Initialize()
		{	
			initialized = false;			
			initializing = true;	

			RemoveFromSolver(null);
			generators.Clear();
			
			AddGenerator(ObiPlantGenerator.GeneratorType.Trunk,"Trunk",-1);

			active = new bool[pooledParticles];
			positions = new Vector3[pooledParticles];
			orientations = new Quaternion[pooledParticles];
			velocities = new Vector3[pooledParticles];
			angularVelocities = new Vector3[pooledParticles];
			invMasses  = new float[pooledParticles];
			invRotationalMasses = new float[pooledParticles];
			principalRadii = new Vector3[pooledParticles];
			phases = new int[pooledParticles];
			restPositions = new Vector4[pooledParticles];
			restOrientations = new Quaternion[pooledParticles];
			colors = new Color[pooledParticles];

			parentIndices = new int[pooledParticles];
			parentOffsets = new float[pooledParticles];
			levels = new int[pooledParticles];


			// Generate root particle:
			active[0] = true;
			invMasses[0] = 0;
			invRotationalMasses[0] = 0;

			restPositions[0] = positions[0] = Vector3.zero;
			restPositions[0][3] = 1;

			restOrientations[0] = orientations[0] = Quaternion.AngleAxis(90,Vector3.forward);
			principalRadii[0] = Vector3.zero;
			phases[0] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
			colors[0] = Color.white;

			parentIndices[0] = -1;
			parentOffsets[0] = 1;
			levels[0] = 0; 
			meristems = new List<Meristem>(){new Meristem(0,0,Meristem.GrowthType.Growing,0,1,0)};
			usedParticles = 1;
			
			StretchShearConstraints.Clear();
			ObiStretchShearConstraintBatch stretchBatch = new ObiStretchShearConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			StretchShearConstraints.AddBatch(stretchBatch);

			BendTwistConstraints.Clear();
			ObiBendTwistConstraintBatch twistBatch = new ObiBendTwistConstraintBatch(false,false,MIN_YOUNG_MODULUS,MAX_YOUNG_MODULUS);
			BendTwistConstraints.AddBatch(twistBatch);

			initializing = false;
			initialized = true;

			yield return 0;

		}*/

        public int AddGenerator(ObiPlantGenerator.GeneratorType type, string name, int parent)
        {

            // Insert right after the parent:
            int index = Mathf.Clamp(parent + 1, 0, generators.Count);

            ObiPlantGenerator gen = new ObiPlantGenerator(type, name, index, parent);

            // Update indices:
            for (int i = index; i < generators.Count; ++i)
            {
                generators[i].id++;
            }

            generators.Insert(index, gen);

            return index;
        }

        public void RemoveGenerator(int index)
        {

            if (index >= 0 && index < generators.Count)
            {

                generators.RemoveAt(index);

                // Update indices:
                for (int i = index; i < generators.Count; ++i)
                {

                    generators[i].id--;

                    if (generators[i].parent >= index)
                        generators[i].parent--;
                }
            }
        }

        public bool IsGrowing()
        {

            // Check all meristems: if none is growing, bailout.
            for (int m = 0; m < meristems.Count; ++m)
            {
                if ((meristems[m].growth & Meristem.GrowthType.Growing) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int GetGeneratorLevel(ObiPlantGenerator generator)
        {

            int level = 0;
            int prev = generator.parent;

            while (prev >= 0)
            {
                level++;
                prev = prev < generators.Count ? generators[prev].parent : -1;
            }

            return level;

        }

        public void InstantGrow()
        {

            //return;
            /*if (InSolver)
				RemoveFromSolver();
			

			ResetActor();

			AddToSolver();*/

            while (IsGrowing() && solver != null)
            {

                float deltaTime = 0.1f;

                //				Solver.UpdateColliders();
                //solver.Substep(null, deltaTime, deltaTime, 1, 0);
                //solver.Render(1, deltaTime, 0);
            }

            UpdateMesh();

            //RemoveFromSolver(null);
        }

        private int AppendParticle(int parentIndex, float parentOffset, Color color, Quaternion orientation, bool split)
        {

            /*if (usedParticles < particleIndices.Length){

				int parentSolverIndex = particleIndices[parentIndex];
				int newSolverIndex = particleIndices[usedParticles];
	
				// Add a new apical particle at the top:
				active[usedParticles] = true;
				phases[usedParticles] = solver.phases[newSolverIndex] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
				colors[usedParticles] = color;
				levels[usedParticles] = levels[parentIndex] + (split?1:0); 
				parentIndices[usedParticles] = parentSolverIndex;
				parentOffsets[usedParticles] = parentOffset;

				// particle size:
				principalRadii[usedParticles] = solver.principalRadii[newSolverIndex] = Vector3.zero;
	
				// init particle mass/rotational mass to a very small, non-zero value.
				invMasses[usedParticles] = solver.invMasses[newSolverIndex] = float.MaxValue;
				invRotationalMasses[usedParticles] = solver.invRotationalMasses[newSolverIndex] = float.MaxValue;
	
				// position:
				Vector3 newPosition = (Vector3)solver.positions[parentSolverIndex] + (solver.orientations[parentSolverIndex] * Vector3.right) * solver.principalRadii[parentSolverIndex][0];
				Vector3 newRestPosition = (Vector3)solver.restPositions[parentSolverIndex] + (solver.restOrientations[parentSolverIndex] * Vector3.right) * solver.principalRadii[parentSolverIndex][0];

				solver.positions[newSolverIndex] = newPosition;
				restPositions[usedParticles] = solver.restPositions[newSolverIndex] = new Vector4(newRestPosition.x,newRestPosition.y,newRestPosition.z,1);

				// orientation:
				solver.orientations[newSolverIndex] = solver.orientations[parentSolverIndex] * orientation;
				restOrientations[usedParticles] = solver.restOrientations[newSolverIndex] = solver.restOrientations[parentSolverIndex] * orientation;

				PushDataToSolver(ParticleData.ACTIVE_STATUS);
	
				// Add twist/bend and stretch/shear constraints:
				ObiBendTwistConstraintBatch twistBatch = BendTwistConstraints.GetFirstBatch();
				Quaternion darboux = ObiUtils.RestDarboux(solver.orientations[parentSolverIndex],solver.orientations[newSolverIndex]);	
				twistBatch.AddConstraint(parentSolverIndex,newSolverIndex,darboux,Vector3.one);

				ObiStretchShearConstraintBatch stretchBatch = StretchShearConstraints.GetFirstBatch();
				stretchBatch.AddConstraint(parentSolverIndex,newSolverIndex,solver.principalRadii[parentSolverIndex][0],Quaternion.LookRotation(Vector3.right),Vector3.one);

				return usedParticles++;
			}*/
            return usedParticles;
        }

        private float Phototropism(int plantIndex, ObiPlantGenerator gen, float deltaTime)
        {

            int solverIndex = solverIndices[plantIndex];

            float growthBoost = 1;

            // Determine particle position relative to light, and if it is in shadow or not.
            /*if (gen.phototropism > 0 || gen.lightGrowthBoost > 0){

				Vector3 particleToLight = -GameObject.FindObjectOfType<Light>().transform.forward;

				Ray ray = new Ray(solver.positions[solverIndex],particleToLight);
				if (!Physics.Raycast(ray, 100)){

					// Lit particles experiment a growth boost:
					growthBoost = gen.lightGrowthBoost;

					// Grow towards the light:
					int parentIndex = parentIndices[plantIndex];
					if (parentIndex >= 0 && gen.phototropism > 0){

						Quaternion toLight = Quaternion.LookRotation(particleToLight) * Quaternion.LookRotation(-Vector3.right);
						Quaternion phototropismOrientation = Quaternion.Slerp(solver.orientations[solverIndex], toLight, gen.phototropism * deltaTime);
						solver.restOrientations[solverIndex] = solver.orientations[solverIndex] = phototropismOrientation;
		
						// Update rest state of bend/twist constraint:
						ObiBendTwistConstraintBatch twistBatch = BendTwistConstraints.GetFirstBatch();
						Quaternion darboux = ObiUtils.RestDarboux(solver.orientations[particleIndices[parentIndex]],solver.orientations[solverIndex]);
						twistBatch.restDarbouxVectors[ plantIndex-1 ] = darboux;
					}
				}

			}*/

            return growthBoost;
        }

        private void SurfaceAdaptation(int plantIndex, ObiPlantGenerator gen, float deltaTime)
        {

            int solverIndex = solverIndices[plantIndex];
            int parentIndex = parentIndices[plantIndex];

            /*if (parentIndex >= 0 && closestSurfaces[plantIndex].distance < 10){

				Vector3 particleX = solver.orientations[solverIndex] * Vector3.right;
				Vector3 particleY = solver.orientations[solverIndex] * Vector3.up;

				float parallel =  Vector3.Dot(particleX, closestSurfaces[plantIndex].direction);
				float twist =     Vector3.Dot(particleY, closestSurfaces[plantIndex].direction);

				Vector3 axis = Vector3.Cross(closestSurfaces[plantIndex].direction,particleX);

				// Orient particle towards the tropism direction:
				Quaternion surfaceOrientation = Quaternion.AngleAxis(twist    * gen.thigmotropism * deltaTime, particleX) * 
												Quaternion.AngleAxis(parallel * gen.thigmotropism * deltaTime, axis);

				solver.restOrientations[solverIndex] = solver.orientations[solverIndex] = surfaceOrientation * solver.orientations[solverIndex];

				// Update rest state of bend/twist constraint:
				ObiBendTwistConstraintBatch twistBatch = BendTwistConstraints.GetFirstBatch();
				Quaternion darboux = ObiUtils.RestDarboux(solver.orientations[particleIndices[parentIndex]],solver.orientations[solverIndex]);
				twistBatch.restDarbouxVectors[ plantIndex-1 ] = darboux;
			}*/
        }

        private void UpdateParticleConstraintsRestState(int plantIndex)
        {

            /*int parentIndex = parentIndices[plantIndex];
			int solverIndex = particleIndices[plantIndex];

			if (parentIndex >= 0){
				int parentSolverIndex = particleIndices[parentIndex];

				// Calculate attachment point:
				Vector3 parentAttachmentPoint = (Vector3)solver.positions[parentIndex] + (solver.orientations[parentSolverIndex] * Vector3.right) * solver.principalRadii[parentSolverIndex][0] * parentOffsets[plantIndex];
				Vector3 parentRestAttachmentPoint = (Vector3)solver.restPositions[parentIndex] + (solver.restOrientations[parentSolverIndex] * Vector3.right) * solver.principalRadii[parentSolverIndex][0] * parentOffsets[plantIndex];

				// Update particle current and rest position:
				Vector3 growthPosition = parentAttachmentPoint + (solver.orientations[solverIndex] * Vector3.right) * solver.principalRadii[solverIndex][0];
				Vector3 growthRestPosition = parentRestAttachmentPoint + (solver.restOrientations[solverIndex] * Vector3.right) * solver.principalRadii[solverIndex][0];
		
				solver.positions[solverIndex] = growthPosition;
				restPositions[plantIndex] = solver.restPositions[solverIndex] = new Vector4(growthRestPosition.x,growthRestPosition.y,growthRestPosition.z,1);
		
				// Update rest state of stretch/shear constraint:
				int solverParentIndex = particleIndices[parentIndex];
				ObiStretchShearConstraintBatch stretchBatch = StretchShearConstraints.GetFirstBatch();
				Vector3 stretchDirection = solver.restPositions[solverIndex] - solver.restPositions[solverParentIndex];
		
				stretchBatch.restOrientations [plantIndex - 1] = Quaternion.LookRotation(Quaternion.Inverse(solver.restOrientations[solverParentIndex]) * stretchDirection);
				stretchBatch.restLengths [plantIndex - 1] = stretchDirection.magnitude;

			}*/
        }

        private void GrowParticle(int plantIndex, float apicalDelta, float maxLength, float maxThickness, float massMultiplier, float rigidness)
        {

            /*float maxRatio = maxThickness / maxLength;
			int solverIndex = particleIndices[plantIndex];

			principalRadii[plantIndex] += new Vector3(1,maxRatio,maxRatio) * apicalDelta * 0.5f;
			principalRadii[plantIndex][0] = Mathf.Min(principalRadii[plantIndex][0],maxLength * 0.5f);
			principalRadii[plantIndex][1] = Mathf.Min(principalRadii[plantIndex][1],maxThickness * 0.5f);
			principalRadii[plantIndex][2] = Mathf.Min(principalRadii[plantIndex][2],maxThickness * 0.5f);

			solver.principalRadii[solverIndex] = principalRadii[plantIndex];

			// TODO: particles that are direct children of growing particles should still update their constraints:
			UpdateParticleConstraintsRestState(plantIndex);

			// Update particle mass and rotational mass:
			if (solver.invMasses[solverIndex] > 0){

				float volume = ObiUtils.EllipsoidVolume(principalRadii[plantIndex]);

				invMasses[plantIndex] = solver.invMasses[solverIndex] = 					1.0f/(volume * Mathf.Max(0.01f,massMultiplier));	
				invRotationalMasses[plantIndex] = solver.invRotationalMasses[solverIndex] = 1.0f/(volume * Mathf.Max(0.01f,rigidness));

			}*/

        }

        private void Split(Meristem meristem, ObiPlantGenerator gen, ObiPlantGenerator parent, float growthDelta, float thickness, float deltaTime)
        {

            // Get normalized length of branch:
            float norm = meristem.length / parent.length;
            float nextNorm = (meristem.length + growthDelta) / parent.length;

            // See if we must generate any of the nodes:
            for (int i = 0; i < gen.nodes.Count; ++i)
            {

                Node node = gen.nodes[i];

                // If the node must be generated here, do it:
                if (norm <= node.normPos &&
                    nextNorm > node.normPos)
                {

                    // Find normalized node factor (0,1) between meristem lengths pre- and post- growth.
                    float blend = (node.normPos - norm) / (nextNorm - norm);

                    // Calculate percentage of final length for pre- and post- growth lengths:
                    float prePctg = 0; //principalRadii[meristem.particleIndex].x / parent.MaxParticleLength;
                    float postPctg = 0;//(principalRadii[meristem.particleIndex].x + growthDelta) / parent.MaxParticleLength;

                    // blend the two percentages based on normalized position:
                    float offset = Mathf.Lerp(prePctg, postPctg, blend);

                    // find amount of delta time left. here we interpolate between original delta time, and delta time once we have consumed the time taken for the particle to grow.
                    float remainingDelta = Mathf.Lerp(deltaTime, deltaTime - growthDelta / gen.growthRate, blend);

                    // Append a particle and create a new meristem referencing it. Note: the offset is mapped from (0,1) to (-1,1).
                    int newIndex = AppendParticle(meristem.particleIndex, (offset - 0.5f) * 2, gen.color, node.orientation, true);
                    meristems.Add(new Meristem(newIndex, gen.id, Meristem.GrowthType.Growing, 0, thickness, remainingDelta));
                }
            }

        }

        private void GrowMeristem(Meristem meristem, ObiPlantGenerator gen)
        {

            if ((meristem.growth & Meristem.GrowthType.Growing) != 0)
            {

                float remainingGrowth = gen.length - meristem.length;

                if (remainingGrowth > 0)
                {

                    // Calculate growth delta using rate (meters/second)
                    float growthDelta = Mathf.Min(gen.growthRate * meristem.growthBoost * meristem.availableGrowthTime, remainingGrowth);

                    // Append particles until the meristem has grown the desired length, or we run out of particles
                    while (growthDelta > 0 && usedParticles < solverIndices.count)
                    {

                        // Get index of meristemic particle:
                        int i = meristem.particleIndex;

                        float particleGrowthDelta = 0;//Mathf.Max(0,Mathf.Min(gen.MaxParticleLength - principalRadii[i].x * 2,growthDelta));

                        // If the meristemic particle can grow more, make it grow:
                        if (particleGrowthDelta > 0)
                        {

                            // Evaluate thickness curve:
                            float shapeEvPoint = meristem.length / gen.length;
                            float maxParticleThickness = Mathf.Max(0.01f, gen.shape.Evaluate(shapeEvPoint) * meristem.thickness * gen.thickness);

                            // Consider if we should perform a split:
                            for (int j = 0; j < generators.Count; ++j)
                            {

                                ObiPlantGenerator g = generators[j];

                                if (g.type == ObiPlantGenerator.GeneratorType.Branch)
                                {

                                    int parentId = g.parent >= 0 ? generators[g.parent].id : -1;

                                    // Only split meristems of the generator's parent id:
                                    if (meristem.generatorId == parentId)
                                        Split(meristem, g, generators[g.parent], particleGrowthDelta, maxParticleThickness, meristem.availableGrowthTime);

                                }
                            }
                            // Calculate time taken by the particle to grow:
                            float particleGrowthTime = particleGrowthDelta / (gen.growthRate * meristem.growthBoost);

                            // Grow the particle:
                            GrowParticle(i, particleGrowthDelta, gen.MaxParticleLength, maxParticleThickness, gen.massMultiplier, gen.rigidness);

                            // Update growth delta:
                            growthDelta -= particleGrowthDelta;

                            // Increase meristem length and consume time taken to grow:
                            meristem.availableGrowthTime -= particleGrowthTime;
                            meristem.length += particleGrowthDelta;

                            // Calculate surface adaptation:
                            SurfaceAdaptation(i, gen, particleGrowthTime);

                            // Calculate phototropism:
                            meristem.growthBoost = Phototropism(i, gen, particleGrowthTime);

                            // Use new meristem growth boost to recalculate remaining growth and growth delta:
                            remainingGrowth = gen.length - meristem.length;
                            growthDelta = Mathf.Min(gen.growthRate * meristem.growthBoost * meristem.availableGrowthTime, remainingGrowth);
                        }
                        // Append a (randomly rotated) new particle at the end of the previous one and update meristem:
                        else
                        {
                            Quaternion randomOrientation = Quaternion.Slerp(Quaternion.identity, UnityEngine.Random.rotation, gen.randomness);
                            meristem.particleIndex = AppendParticle(i, 1, gen.color, randomOrientation, false);
                        }

                    }
                }
                else
                {
                    meristem.growth = Meristem.GrowthType.Tip;
                }

            }

        }

        private void ApplyGrowthGenerators(Meristem meristem)
        {

            for (int i = 0; i < generators.Count; ++i)
            {

                ObiPlantGenerator gen = generators[i];

                if (gen.Type == ObiPlantGenerator.GeneratorType.Trunk ||
                    gen.Type == ObiPlantGenerator.GeneratorType.Branch)
                {

                    // Only grow meristems with the same id:
                    if (meristem.generatorId == gen.id)
                    {

                        gen.LoadRNGState();
                        GrowMeristem(meristem, gen);
                        gen.StoreRNGState();
                    }

                }
            }

        }

        //public override void OnSolverStepEnd(float deltaTime){

        // If the tree is no longer growing, skip meristem growth.
        /*if (IsGrowing())
        {
            BendTwistConstraints.RemoveFromSolver(null);
            StretchShearConstraints.RemoveFromSolver(null);

            // Initialize all meristems available growth time:
            for (int i = 0; i < meristems.Count; ++i)
                meristems[i].availableGrowthTime = deltaTime;

            // Calculate meristemic growth. New lateral meristems might be appended while iterating.
            for (int i = 0; i < meristems.Count; ++i)
                ApplyGrowthGenerators(meristems[i]);

            BendTwistConstraints.AddToSolver(null);
            StretchShearConstraints.AddToSolver(null);
        }

        if (Application.isPlaying)
            UpdateMesh();*/

        //}	

        void Solver_OnCollision(object sender, ObiNativeContactList e)
        {
            for (int i = 0; i < usedParticles; ++i)
            {
                closestSurfaces[i].distance = float.MaxValue;
            }

            if (e == null) return;

            for (int i = 0; i < e.count; ++i)
            {
                Oni.Contact contact = e[i];
                ObiSolver.ParticleInActor pa = m_Solver.particleToActor[contact.bodyA];

                if (pa.actor == this)
                {

                    /*Vector3 point = contact.point;
					Vector3 normal = contact.normal;
			
					Debug.DrawRay(point,normal.normalized * contact.distance,(contact.distance < 0.01f) ? Color.red : Color.green);*/

                    if (contact.distance < closestSurfaces[pa.indexInActor].distance)
                    {
                        closestSurfaces[pa.indexInActor].distance = contact.distance;
                        closestSurfaces[pa.indexInActor].direction = -contact.normal;
                    }

                }
            }
        }

        /**
 		* Resets mesh to its original state.
 		*/
        /*public override void ResetActor(){

			if (!initialized) 
				return;

			StretchShearConstraints.GetFirstBatch().Clear();
			BendTwistConstraints.GetFirstBatch().Clear();
	
			for(int i = 0; i < active.Length; ++i){

				active[i] = false;
				parentIndices[i] = -1;
				parentOffsets[i] = 1;
				levels[i] = 0; 

				invMasses[i] = 0;
				invRotationalMasses[i] = 0;

				restPositions[i] = positions[i] = Vector3.zero;
				restOrientations[i] = orientations[i] = Quaternion.identity;
				principalRadii[i] = Vector3.zero;
				phases[i] = 0;
				colors[i] = Color.white;

			}

			active[0] = true;
			usedParticles = 1;
			restOrientations[0] = orientations[0] = Quaternion.AngleAxis(90,Vector3.forward);
			restPositions[0][3] = 1;
			phases[0] = Oni.MakePhase(1,selfCollisions?Oni.ParticlePhase.SelfCollide:0);
	
			PushDataToSolver(ParticleData.ALL);
			
			if (particleIndices != null){
				for(int i = 0; i < active.Length; ++i){
					solver.renderablePositions[particleIndices[i]] = positions[i];
				}
			}

			meristems = new List<Meristem>(){new Meristem(0,0,Meristem.GrowthType.Growing,0,1,0)};

			GenerateNodes();
		}*/

    }
}



