using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Character
{

    public class Pronoun : MonoBehaviour
    {
        const int NUM_THREAD_X = 8; 
        const int NUM_THREAD_Y = 1; 
        const int NUM_THREAD_Z = 1; 
        public int numberOfParticles = 32678; 
        public ComputeShader ParticleComputeShader; 
        public Shader ParticleRenderShader;  
        public Vector3 Gravity = new Vector3(0.0f, -1.0f, 0.0f); // 重力
        public Vector3 AreaSize = Vector3.one * 10.0f;            
        public float SphereSize = 5f;
        public float speedScale = 1f;
        public Texture2D ParticleTex;          
        public float ParticleSize = 0.05f;
        public Camera RenderCam;
        ComputeBuffer particleBuffer;     
        Material particleRenderMat;  
        private Vector3 _prevOffset;
        public Color particleColor;
        public Vector3 attractor;
        
        // Start is called before the first frame update
        void Start()
        {
            _prevOffset = Vector3.zero;
            particleBuffer = new ComputeBuffer(numberOfParticles, Marshal.SizeOf(typeof(ParticleData)));
            var pData = new ParticleData[numberOfParticles];
            for (int i = 0; i < pData.Length; i++)
            {
                Vector3 speedVec = Random.insideUnitSphere * speedScale; 
                pData[i].Velocity = new Vector3(speedVec.x, speedVec.y, speedVec.z);
                Vector3 spherePosition = Random.insideUnitSphere * SphereSize; 
                Vector3 position = new Vector3(spherePosition.x, spherePosition.y, spherePosition.z);
                pData[i].Position = position;
            }
            particleBuffer.SetData(pData);
            pData = null;
            particleRenderMat = new Material(ParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
        }

        // Update is called once per frame
        void OnRenderObject()
        {
            ComputeShader cs = ParticleComputeShader;
            int numThreadGroup = numberOfParticles / NUM_THREAD_X;
            int kernelId = cs.FindKernel("CSMain");
            cs.SetFloat("_TimeStep", Time.deltaTime);
            cs.SetVector("_Gravity", Gravity);
            cs.SetFloats("_AreaSize", new float[3] { AreaSize.x, AreaSize.y, AreaSize.z });
            cs.SetInt("_ParticleCount", particleBuffer.count);
            cs.SetVector("_CurrentOffset", transform.position);
            cs.SetVector("_PreviousOffset", _prevOffset);
            cs.SetVector("_AttractorPos", attractor);
            cs.SetBuffer(kernelId, "_ParticleBuffer", particleBuffer);
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);
            var inverseViewMatrix = RenderCam.worldToCameraMatrix.inverse;
            Material m = particleRenderMat;
            m.SetPass(0); 
            m.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            m.SetTexture("_MainTex", ParticleTex);
            m.SetFloat("_ParticleSize", ParticleSize);
            m.SetBuffer("_ParticleBuffer", particleBuffer);
            m.SetColor("_ParticleColor", particleColor);
            Graphics.DrawProceduralNow(MeshTopology.Points, numberOfParticles);

            _prevOffset = transform.position;
        }
        
    }
}
