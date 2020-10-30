using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class Where : MonoBehaviour
{
    public struct WhereLineData
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 velocity;
    }
    const int NUM_THREAD_X = 8; 
    const int NUM_THREAD_Y = 1; 
    const int NUM_THREAD_Z = 1;
    public float halfSize = 500;
    public float stepSize = 10;
    private int _numberOfLinePerSide;
    public Shader LineRender;
    public ComputeShader WhereComputeShader;
    private ComputeBuffer WhereLinesBuffer;
    private ComputeBuffer WhereLinesRenderBuffer;
    private Material lineRenderMat;
    private int _totalLines;
    public float maxHalf;
    public float speedScale = 50;
    public float xRotationSpeed;
    public float yRotationSpeed;
    public float zRotationSpeed;
    private Vector3 _startRotation;
    
    // Start is called before the first frame update
    void Start()
    {
        _startRotation = Vector3.zero;
        _numberOfLinePerSide = (int)(halfSize * 2 / stepSize);
        _totalLines = 3*_numberOfLinePerSide * _numberOfLinePerSide;
        WhereLinesBuffer = new ComputeBuffer(_totalLines, Marshal.SizeOf(typeof(WhereLineData)));
        WhereLinesRenderBuffer = new ComputeBuffer(_totalLines, Marshal.SizeOf(typeof(WhereLineData)));
        var lineData = new WhereLineData[_totalLines];
        //x plane going up
        int counter = 0;
        for (int i = 0; i < _numberOfLinePerSide; i++)
        {
            for (int j = 0; j < _numberOfLinePerSide; j++)
            {
                float xpos = -halfSize + i * stepSize;
                float ypos = -halfSize + j * stepSize;
                Vector3 start = new Vector3(xpos, ypos, -halfSize);
                Vector3 end = new Vector3(xpos, ypos, halfSize);
                float velx = Random.Range(0f, 2f) - 1f;
                velx *= speedScale; 
                Vector3 vel = new Vector3(velx, velx, 0);
                int index = i * _numberOfLinePerSide + j;
                lineData[index].start = start;
                lineData[index].end = end;
                lineData[index].velocity = vel;
                counter += 1;
            }
        }
        
        for (int i = 0; i < _numberOfLinePerSide; i++)
        {
            for (int j = 0; j < _numberOfLinePerSide; j++)
            {
                float zpos = -halfSize + i * stepSize;
                float ypos = -halfSize + j * stepSize;
                Vector3 start = new Vector3(-halfSize, ypos, zpos);
                Vector3 end = new Vector3(halfSize, ypos, zpos);
                float velz = Random.Range(0f, 2f) - 1f;
                velz *= speedScale;
                Vector3 vel = new Vector3(0, velz, velz);
                int index = counter;
                lineData[index].start = start;
                lineData[index].end = end;
                lineData[index].velocity = vel;
                counter += 1;
            }
        }
        
        for (int i = 0; i < _numberOfLinePerSide; i++)
        {
            for (int j = 0; j < _numberOfLinePerSide; j++)
            {
                float xpos = -halfSize + i * stepSize;
                float zpos = -halfSize + j * stepSize;
                Vector3 start = new Vector3(xpos, -halfSize, zpos);
                Vector3 end = new Vector3(xpos, halfSize, zpos);
                float vely = Random.Range(0f, 2f) - 1f;
                vely *= speedScale;
                Vector3 vel = new Vector3(0, 0, 0);
                if (Random.Range(0f, 1f) > 0.5)
                {
                    vel.x = vely;
                }
                else
                {
                    vel.z = vely;
                }
                int index = counter;
                lineData[index].start = start;
                lineData[index].end = end;
                lineData[index].velocity = vel;
                counter += 1;
            }
        }
        
        WhereLinesBuffer.SetData(lineData);
        WhereLinesRenderBuffer.SetData(lineData);
        lineData = null;
        lineRenderMat = new Material(LineRender);
        lineRenderMat.hideFlags = HideFlags.HideAndDontSave;
    }

    void OnRenderObject()
    {
        ComputeShader cs = WhereComputeShader;
        int numThreadGroup = _totalLines / NUM_THREAD_X;
        int kernelId = cs.FindKernel("CSMain");
        cs.SetFloat("_TimeStep", Time.deltaTime);
        cs.SetFloat("_HalfSize", halfSize);
        cs.SetVector("_Offset", transform.position);
        cs.SetVector("_CurrentRotation", transform.rotation.eulerAngles);
        cs.SetBuffer(kernelId,"_WhereLineBuffer", WhereLinesBuffer);
        cs.SetBuffer(kernelId,"_WhereLineRenderBuffer", WhereLinesRenderBuffer);
        cs.Dispatch(kernelId, numThreadGroup, 1, 1);
        Material l = lineRenderMat;
        l.SetPass(0);
        l.SetBuffer("_ConnectionBuffer", WhereLinesRenderBuffer);
        Graphics.DrawProceduralNow(MeshTopology.Lines, _totalLines);
        _startRotation.x = _startRotation.x + xRotationSpeed;
        _startRotation.y = _startRotation.y + yRotationSpeed;
        _startRotation.z = _startRotation.z + zRotationSpeed;
        transform.eulerAngles = _startRotation;
    }
}
