﻿using System;
using UnityEngine;
using System.Numerics;
using System.Runtime.InteropServices;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Character 
{
    public class Who : MonoBehaviour
    {
        public struct CirclePointData
        {
            public Vector3 Velocity;
            public Vector3 Position;
        }

        public struct CircleSegment
        {
            public Vector3 Start;
            public Vector3 End;
        }
            
        // Compute Shader Initialization
        const int NUM_THREAD_X = 8; 
        const int NUM_THREAD_Y = 1; 
        const int NUM_THREAD_Z = 1; 
        public int numberOfCircularPoints = 1000;
        public float radius = 300;
        public Shader LineRender;
        public ComputeShader CircleComputeShader;
        public Camera RenderCam;
        private ComputeBuffer CirclePointBuffer;
        private ComputeBuffer CircleSegmentBuffer;
        private ComputeBuffer CircleSegmentRenderBuffer; //This Buffer handles all the position and rotation changes 
        private Material lineRenderMat;
        private float xRotationSpeed;
        private float yRotationSpeed;
        private float zRotationSpeed;
        private Vector3 _startRotation;
        // For moving around the position
        
        void Start()
        {
            xRotationSpeed = Random.Range(-0.03f, 0.03f);
            yRotationSpeed = Random.Range(-0.03f, 0.03f);
            zRotationSpeed = Random.Range(-0.03f, 0.03f);
            transform.eulerAngles = new Vector3(Random.Range(-90f,90f), Random.Range(-90f,90f), Random.Range(-90f,90f));
            _startRotation = transform.eulerAngles;
            CirclePointBuffer = new ComputeBuffer(numberOfCircularPoints, Marshal.SizeOf(typeof(CirclePointData)));
            CircleSegmentBuffer = new ComputeBuffer(numberOfCircularPoints, Marshal.SizeOf(typeof(CircleSegment)));
            CircleSegmentRenderBuffer = new ComputeBuffer(numberOfCircularPoints, Marshal.SizeOf(typeof(CircleSegment)));
            var pData = new CirclePointData[numberOfCircularPoints];
            var cData = new CircleSegment[numberOfCircularPoints];
            for (int i = 0; i < pData.Length; i++)
            {
                //Vector3 velocity = new Vector3(0, 0, Random.Range(-15f, 15f));
                Vector3 center = new Vector3(0, 0,  0);
                float theta = ((float)i / pData.Length) * Mathf.PI * 2;
                float xPos = Mathf.Cos(theta) * radius;
                float yPos = 0; 
                float zPos = Mathf.Sin(theta) * radius;
                //float zPos = 0;
                Vector3 position = new Vector3(xPos, yPos, zPos);
                //Vector3 velocity =  position - center;
                Vector3 velocity =  Vector3.zero;
                velocity = velocity.normalized;
                pData[i].Velocity = velocity;
                pData[i].Position = position;
            }
            for (int i = 0; i < cData.Length-1; i++)
            {
                Vector3 start = pData[i].Position;
                Vector3 end = pData[i+1].Position;
                cData[i].Start = start;
                cData[i].End = end;
            }
            cData[cData.Length - 1].Start = pData[cData.Length - 1].Position;
            cData[cData.Length - 1].End = pData[0].Position;
            CirclePointBuffer.SetData(pData);
            CircleSegmentBuffer.SetData(cData);
            CircleSegmentRenderBuffer.SetData(cData);
            pData = null;
            cData = null; 
            lineRenderMat = new Material(LineRender);
            lineRenderMat.hideFlags = HideFlags.HideAndDontSave;
        }

        void OnRenderObject()
        {
            ComputeShader cs = CircleComputeShader;
            int numThreadGroup = numberOfCircularPoints / NUM_THREAD_X;
            int kernelId = cs.FindKernel("CSMain");
            cs.SetVector("_CurrentOffset", transform.position);
            cs.SetVector("_CurrentRotation", transform.rotation.eulerAngles);
            cs.SetFloat("_TimeStep", Time.deltaTime);
            cs.SetInt("_CirclePointCount", numberOfCircularPoints);
            cs.SetBuffer(kernelId, "_CirclePointBuffer", CirclePointBuffer);
            cs.SetBuffer(kernelId, "_CircleSegmentBuffer", CircleSegmentBuffer);
            cs.SetBuffer(kernelId, "_CircleSegmentRenderBuffer", CircleSegmentRenderBuffer);
            cs.Dispatch(kernelId, numThreadGroup, 1, 1);
            Material l = lineRenderMat;
            l.SetPass(0);
            l.SetBuffer("_ConnectionBuffer", CircleSegmentRenderBuffer);
            Graphics.DrawProceduralNow(MeshTopology.Lines, numberOfCircularPoints);
            _startRotation.x = _startRotation.x + xRotationSpeed;
            _startRotation.y = _startRotation.y + yRotationSpeed;
            _startRotation.z = _startRotation.z + zRotationSpeed;
            transform.eulerAngles = _startRotation;
        }
    }
}



















