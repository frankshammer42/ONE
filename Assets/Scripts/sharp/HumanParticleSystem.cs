using UnityEngine;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;

namespace Character 
{
    public struct ParticleData
    {
        public Vector3 Velocity; 
        public Vector3 Position; 
    };

    public struct ConnectionData
    {
        public Vector3 Start;
        public Vector3 End;
        public int Connect;
    }

    public class HumanParticleSystem : MonoBehaviour
    {
        //TODO: How do we actually decide which thread to use and how many threads to work on?
        const int NUM_THREAD_X = 8; 
        const int NUM_THREAD_Y = 1; 
        const int NUM_THREAD_Z = 1; 
        public int numberOfParticles = 32678; 

        public Color particleColor;

        public ComputeShader ParticleComputeShader; 
        public Shader ParticleRenderShader;  
        public Shader LineRenderShader;

        public Vector3 Gravity = new Vector3(0.0f, -1.0f, 0.0f); // 重力
        public Vector3 AreaSize = Vector3.one * 10.0f;            
        public float SphereSize = 5f;
        public float MaxSphereSize = 20f;
        public float speedScale = 1f;

        public Texture2D ParticleTex;          
        public float ParticleSize = 0.05f; 
        public float LineThreshold = 2.0f;
        public Camera RenderCam; 
        ComputeBuffer particleBuffer;     
        ComputeBuffer connectionBuffer;
        Material particleRenderMat;  
        Material lineRenderMat;
        
        //For moving around objects
        private Vector3 _prevOffset;
        

        void Start()
        {

            _prevOffset = Vector3.zero;
            particleBuffer = new ComputeBuffer(numberOfParticles, Marshal.SizeOf(typeof(ParticleData)));
            connectionBuffer = new ComputeBuffer(numberOfParticles, Marshal.SizeOf(typeof(ConnectionData)));
            var pData = new ParticleData[numberOfParticles];
            var cData = new ConnectionData[numberOfParticles];
            for (int i = 0; i < pData.Length; i++)
            {
                //pData[i].Velocity = Random.insideUnitSphere;
                //pData[i].Position = Random.insideUnitSphere;
                float xSpeed = Random.Range(-0.2f, 0.2f);
                float ySpeed = Random.Range(-0.2f, 0.2f);
                float zSpeed = Random.Range(-0.2f, 0.2f);
                Vector3 speedVec = Random.insideUnitSphere * speedScale; 
                //pData[i].Velocity = new Vector3(xSpeed, ySpeed, zSpeed);
                pData[i].Velocity = new Vector3(speedVec.x, speedVec.y, speedVec.z);
                //pData[i].Velocity = new Vector3(0,0,0);
                //float yPos = Random.Range(0.1f, 1f); 
                //float zPos = Random.Range(0.1f, 1f); 
                Vector3 spherePosition = Random.insideUnitSphere * SphereSize; 
                //Vector3 position = new Vector3(yPos*2, -xPos*2, 0);
                Vector3 position = new Vector3(spherePosition.x, spherePosition.y, spherePosition.z);
                pData[i].Position = position;
                // Set Up Connection Data
            }

            for (int i = 0; i < cData.Length; i++)
            {
                cData[i].Start = new Vector3(0, 0, 0);
                cData[i].End = new Vector3(0, 0, 0);
                cData[i].Connect = 0;
            }
            
            // コンピュートバッファに初期値データをセット
            particleBuffer.SetData(pData);
            connectionBuffer.SetData(cData);
            
            pData = null;
            cData = null;
            // パーティクルをレンダリングするマテリアルを作成
            particleRenderMat = new Material(ParticleRenderShader);
            particleRenderMat.hideFlags = HideFlags.HideAndDontSave;
            
            lineRenderMat = new Material(LineRenderShader);
            lineRenderMat.hideFlags = HideFlags.HideAndDontSave;
        }

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
            cs.SetFloat("_Threshold", LineThreshold);
            cs.SetFloat("_SphereSize", MaxSphereSize);
            //cs.SetFloat("_SphereSize", SphereSize);
            cs.SetBuffer(kernelId, "_ParticleBuffer", particleBuffer);
            // Set Connection Buffer
            cs.SetBuffer(kernelId, "_ConnectionBuffer", connectionBuffer);
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

            Material l = lineRenderMat;
            l.SetPass(0);
            l.SetMatrix("_InvViewMatrix", inverseViewMatrix);
            l.SetBuffer("_ConnectionBuffer", connectionBuffer);
            Graphics.DrawProceduralNow(MeshTopology.Lines, numberOfParticles);
            
            //Update Offset 
            _prevOffset = transform.position;
        }

        void OnDestroy()
        {
            if (particleBuffer != null)
            {
                particleBuffer.Release();
            }

            if (connectionBuffer != null)
            {
                connectionBuffer.Release();
            }

            if (particleRenderMat != null)
            {
                DestroyImmediate(particleRenderMat);
            }
            
        }
    }
}