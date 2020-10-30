using System;
using UnityEngine;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Character 
{
    public class Wolrd : MonoBehaviour
    {

        public struct WorldSegment 
        {
            public Vector3 Start;
            public Vector3 End;
            public float speed;
        }
            
        // Compute Shader Initialization
        const int NUM_THREAD_X = 8; 
        const int NUM_THREAD_Y = 1; 
        const int NUM_THREAD_Z = 1; 
        public int numberOfSpherePoints = 1000;
        public float radius = 300;
        public Shader LineRender;
        public ComputeShader WorldComputeShader;
        public Camera RenderCam;
        private ComputeBuffer WolrdSegmentBuffer;
        private ComputeBuffer WorldSegmentRenderBuffer; //This Buffer handles all the position and rotation changes 
        private Material lineRenderMat;
        private float xRotationSpeed;
        private float yRotationSpeed;
        private float zRotationSpeed;
        private Vector3 _startRotation;
        public float lineScale;
        public float MaxScale; 
        // For moving around the position
        void Start()
        {
            xRotationSpeed = Random.Range(-0.05f, 0.05f);
            yRotationSpeed = Random.Range(-0.05f, 0.05f);
            zRotationSpeed = Random.Range(-0.05f, 0.05f);
            transform.eulerAngles = new Vector3(Random.Range(-90f,90f), Random.Range(-90f,90f), Random.Range(-90f,90f));
            _startRotation = transform.eulerAngles;
            WolrdSegmentBuffer = new ComputeBuffer(numberOfSpherePoints, Marshal.SizeOf(typeof(WorldSegment)));
            WorldSegmentRenderBuffer = new ComputeBuffer(numberOfSpherePoints, Marshal.SizeOf(typeof(WorldSegment)));
            var cData = new WorldSegment[numberOfSpherePoints];
            for (int i = 0; i < cData.Length; i++)
            {
                Vector3 start = Random.onUnitSphere * radius;
                Vector3 end = start + start.normalized*Random.Range(1f, 2f)*lineScale;
                cData[i].Start = start;
                cData[i].End = end;
                cData[i].speed = Random.Range(0.1f, 0.5f);
            }
            WolrdSegmentBuffer.SetData(cData);
            WorldSegmentRenderBuffer.SetData(cData);
            cData = null; 
            lineRenderMat = new Material(LineRender);
            lineRenderMat.hideFlags = HideFlags.HideAndDontSave;
        }

        void OnRenderObject()
        {
            ComputeShader cs = WorldComputeShader;
            int numThreadGroup = numberOfSpherePoints / NUM_THREAD_X;
            int kernelId = cs.FindKernel("CSMain");
            cs.SetVector("_CurrentOffset", transform.position);
            cs.SetVector("_CurrentRotation", transform.rotation.eulerAngles);
            cs.SetFloat("_TimeStep", Time.deltaTime);
            cs.SetFloat("_MaxScale", MaxScale);
            cs.SetInt("_SpherePointCount", numberOfSpherePoints);
            cs.SetBuffer(kernelId, "_WorldSegmentBuffer", WolrdSegmentBuffer);
            cs.SetBuffer(kernelId, "_WorldSegmentRenderBuffer", WorldSegmentRenderBuffer);
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);
            Material l = lineRenderMat;
            l.SetPass(0);
            l.SetBuffer("_ConnectionBuffer", WorldSegmentRenderBuffer);
            Graphics.DrawProceduralNow(MeshTopology.Lines, numberOfSpherePoints);
            //_startRotation.x = _startRotation.x + xRotationSpeed;
            //_startRotation.y = _startRotation.y + yRotationSpeed;
            //_startRotation.z = _startRotation.z + zRotationSpeed;
            //transform.eulerAngles = _startRotation;
        }
    }
}



















