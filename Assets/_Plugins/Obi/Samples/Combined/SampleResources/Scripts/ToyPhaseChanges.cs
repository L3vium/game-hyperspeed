using System.Collections.Generic;
using UnityEngine;

namespace Obi.Samples
{
    [RequireComponent(typeof(ObiSolver))]
    public class ToyPhaseChanges : MonoBehaviour
    {
        public ComputeShader phaseChangeShader;
        public ToyController controller;
        public ObiEmitter mossEmitter;
        private GraphicsBuffer toyMaterials;
        private ObiSolver solver;
        int oldMossParticleCount;

        struct ToyMaterial
        {
            public Color color;
            public Color hotColor;
            public Vector4 fluidMaterial;
            public Vector4 fluidMaterial2;
            public Vector4 fluidInterface;
        };

        void OnEnable()
        {
            solver = GetComponent<ObiSolver>();
            solver.OnSimulationStart += Solver_OnSimulationStart;
            solver.OnSubstepsStart += Solver_OnSubstepsStart;
            solver.OnRequestReadback += Solver_OnRequestReadback;
        }

        void OnDisable()
        {
            solver.OnSimulationStart -= Solver_OnSimulationStart;
            solver.OnSubstepsStart -= Solver_OnSubstepsStart;
            solver.OnRequestReadback -= Solver_OnRequestReadback;
        }

        private void InitializeMaterialsList()
        {
            if (toyMaterials == null)
            {
                mossEmitter.solverIndices.AsComputeBuffer<int>();
                mossEmitter.activeParticleCountBuffer.AsComputeBuffer<int>();

                mossEmitter.UpdateFilter();

                List<ToyMaterial> matList = new List<ToyMaterial>();

                // add dummy material to account for material-less particles (materialType = 0)
                matList.Add(new ToyMaterial());

                foreach (ToyTool t in controller.tools)
                {
                    var emitter = t as ToyEmitter;
                    if (emitter != null)
                    {
                        var shape = emitter.GetComponent<ObiEmitterShape>();
                        var em = emitter.GetComponent<ObiEmitter>();
                        matList.Add(new ToyMaterial
                        {
                            color = shape.color,
                            hotColor = emitter.hotColor,
                            fluidMaterial = em.emissionData.fluidMaterial,
                            fluidMaterial2 = em.emissionData.fluidMaterial2,
                            fluidInterface = em.emissionData.fluidInterface
                        }); 
                    }
                }

                toyMaterials = new GraphicsBuffer(GraphicsBuffer.Target.Structured, matList.Count, 80);
                toyMaterials.SetData(matList.ToArray());
            }
        }

        private void OnDestroy()
        {
            toyMaterials.Dispose();
            toyMaterials = null;
        }

        private void Solver_OnSimulationStart(ObiSolver solv, float timeToSimulate, float substepTime)
        {
            InitializeMaterialsList();
            solver.colors.WaitForReadback();
            solver.userData.WaitForReadback();
            solver.invMasses.WaitForReadback();
            solver.fluidMaterials.WaitForReadback();
            solver.fluidMaterials2.WaitForReadback();
            solver.fluidInterface.WaitForReadback();
            solver.principalRadii.WaitForReadback();
            solver.phases.WaitForReadback();

            mossEmitter.activeParticleCountBuffer.WaitForReadback();
            if (mossEmitter.activeParticleCount != oldMossParticleCount)
            {
                mossEmitter.solver.dirtyActiveParticles = true;
                mossEmitter.solver.dirtySimplices |= Oni.SimplexType.Point;
            }
        }

        // color to user data.
        private void Solver_OnSubstepsStart(ObiSolver solv, float timeToSimulate, float substepTime)
        {
            if (solver.backendType == ObiSolver.BackendType.Compute && solver.activeParticles.computeBuffer != null)
            {
                oldMossParticleCount = mossEmitter.activeParticleCount;

                int threadGroups = ComputeMath.ThreadGroupCount(solver.activeParticleCount, 128);
                phaseChangeShader.SetInt("particleCount", solver.activeParticleCount);
                phaseChangeShader.SetFloat("deltaTime", timeToSimulate);
                phaseChangeShader.SetFloat("randomSeed", Time.frameCount % 16535 + Random.value);

                phaseChangeShader.SetBuffer(0, "toyMaterials", toyMaterials);
                phaseChangeShader.SetBuffer(0, "activeParticles", solver.activeParticles.computeBuffer);
                phaseChangeShader.SetBuffer(0, "userData", solver.userData.computeBuffer);
                phaseChangeShader.SetBuffer(0, "colors", solver.colors.computeBuffer);
                phaseChangeShader.SetBuffer(0, "invMasses", solver.invMasses.computeBuffer);
                phaseChangeShader.SetBuffer(0, "fluidMaterials", solver.fluidMaterials.computeBuffer);
                phaseChangeShader.SetBuffer(0, "fluidMaterials2", solver.fluidMaterials2.computeBuffer);
                phaseChangeShader.SetBuffer(0, "fluidInterface", solver.fluidInterface.computeBuffer);
                phaseChangeShader.SetBuffer(0, "phases", solver.phases.computeBuffer);
                phaseChangeShader.SetBuffer(0, "life", solver.life.computeBuffer);

                phaseChangeShader.Dispatch(0, threadGroups, 1, 1);

                // upload particle indices and  number of active particles to GPU. atomically increment when necessary to spawn new particles.
                mossEmitter.solverIndices.Upload();
                mossEmitter.activeParticleCountBuffer.Upload();

                phaseChangeShader.SetBuffer(1, "activeParticles", solver.activeParticles.computeBuffer);
                phaseChangeShader.SetBuffer(1, "principalRadii", solver.principalRadii.computeBuffer);
                phaseChangeShader.SetBuffer(1, "positions", solver.positions.computeBuffer);
                phaseChangeShader.SetBuffer(1, "userData", solver.userData.computeBuffer);
                phaseChangeShader.SetBuffer(1, "fluidMaterials", solver.fluidMaterials.computeBuffer);
                phaseChangeShader.SetBuffer(1, "fluidInterface", solver.fluidInterface.computeBuffer);
                phaseChangeShader.SetBuffer(1, "phases", solver.phases.computeBuffer);
                phaseChangeShader.SetBuffer(1, "life", solver.life.computeBuffer);

                phaseChangeShader.SetBuffer(1, "activeMossParticleCount", mossEmitter.activeParticleCountBuffer.computeBuffer);
                phaseChangeShader.SetBuffer(1, "mossSolverIndices", mossEmitter.solverIndices.computeBuffer);

                phaseChangeShader.Dispatch(1, threadGroups, 1, 1);
            }
        }

        private void Solver_OnRequestReadback(ObiSolver solv)
        {
            if (solver.backendType == ObiSolver.BackendType.Compute)
            {
                solver.colors.Readback();
                solver.userData.Readback();
                solver.invMasses.Readback();
                solver.fluidMaterials.Readback();
                solver.fluidMaterials2.Readback();
                solver.fluidInterface.Readback();
                solver.principalRadii.Readback();
                solver.phases.Readback();

                mossEmitter.activeParticleCountBuffer.Readback();
            }
        }
    }
}