using UnityEngine;

namespace Obi.Samples
{
    public class ToyWall : ToyEmitter
    {
        float strokeLength;
        Vector3 brushHead;
        ObiEmitterShape shape;

        void Start()
        {
            shape = GetComponent<ObiEmitterShape>();
            emitter.OnEmitParticle += ToyWall_OnEmitParticle;
        }

        private void ToyWall_OnEmitParticle(ObiEmitter em, int particleIndex)
        {
            emitter.solver.invMasses[emitter.solverIndices[particleIndex]] = 0;
        }

        public override void OnMouseDown(Vector3 pos)
        {
            brushHead = pos;
        }

        public override void OnMousePressed(Vector3 pos, Vector3 prevPos)
        {
            Vector3 stroke = pos - brushHead;

            float particleSize = emitter.emitterBlueprint.GetParticleSize(Oni.SolverParameters.Mode.Mode2D);
            strokeLength = Vector3.Magnitude(stroke);
            stroke = stroke.normalized;

            int emittedParticles = Mathf.FloorToInt(strokeLength / particleSize);

            for (int i = 0; i < emittedParticles; ++i)
            {
                brushHead += stroke * particleSize;
                emitter.EmitParticle(new EmitPoint(brushHead, Vector3.zero, shape.color));
            }
        }
    }
}
