using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class WindController : MonoBehaviour
{
    // Start is called before the first frame update
    Fluid myFluid;
    int N;
    float visc;
    float diff;
    float dt;
    float[] Vx;
    float[] Vy;
    float[] Vz;
    float[] Vx0;
    float[] Vy0;
    float[] Vz0;
    float[] s;
    float[] density;
    List<GameObject> myVectors;
    void Start()
    {
        int N = 10;
        myFluid = new Fluid(N,.1f, 0f, 0f );
        myVectors = new List<GameObject>();
        for(int i = 0; i < N; i++)
        {
            for(int j = 0; j < N; j++)
            {
                for(int k = 0; k < N; k++)
                {
                    GameObject myThing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    myThing.transform.position = new Vector3(i, j, k);
                    myThing.transform.localScale = new Vector3(.1f,.1f,.1f);
                    myThing.name = "Cube" + i.ToString() + j.ToString() + k.ToString();
                    myVectors.Add(myThing);
                }
            }
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        N = myFluid.size;
        visc = myFluid.visc;
        diff = myFluid.diff;
        dt = myFluid.dt;
        Vx = myFluid.Vx;
        Vy = myFluid.Vy;
        Vz = myFluid.Vz;
        Vx0 = myFluid.Vx0;
        Vy0 = myFluid.Vy0;
        Vz0 = myFluid.Vz0;
        s = myFluid.s;
        density = myFluid.density;
        myFluid.diffuse(1, Vx0, Vx, visc, dt, 4, N);
        myFluid.diffuse(2, Vy0, Vy, visc, dt, 4, N);
        myFluid.diffuse(3, Vz0, Vz, visc, dt, 4, N);

        //myFluid.project(Vx0, Vy0, Vz0, Vx, Vy, 4, N);

        //myFluid.advect(1, Vx, Vx0, Vx0, Vy0, Vz0, dt, N);
        //myFluid.advect(2, Vy, Vy0, Vx0, Vy0, Vz0, dt, N);
        //myFluid.advect(3, Vz, Vz0, Vx0, Vy0, Vz0, dt, N);

        //myFluid.project(Vx, Vy, Vz, Vx0, Vy0, 4, N);

        //myFluid.diffuse(0, s, density, diff, dt, 4, N);
        //myFluid.advect(0, density, s, Vx, Vy, Vz, dt, N);
        //for(int i = 0; i < myVectors.Length; i++)
        //{
        //    Vector3 rotateVec = new Vector3(myFluid.Vx[i], myFluid.Vy[i], myFluid.Vz[i]);
        //    myVectors[i].transform.rotation = Quaternion.LookRotation(rotateVec);
        //}
    }
}

class Fluid {
    
    public int size;
    public float dt;
    public float diff;
    public float visc;

    public float[] s;
    public float[] density;

    public float[] Vx;
    public float[] Vy;
    public float[] Vz;

    public float[] Vx0;
    public float[] Vy0;
    public float[] Vz0;
    public Fluid(int size, float dt, float diffusion, float viscosity)
    {
        this.size = size;
        this.dt = dt;
        this.diff = diffusion;
        this.visc = viscosity;

        this.s = new float[size * size * size];
        this.density = new float[size * size * size];

        this.Vx = new float[size * size * size];
        this.Vy = new float[size * size * size];

        this.Vx0 = new float[size * size * size];
        this.Vy0 = new float[size * size * size];

    }
    public int IX(int x, int y, int z )
    {
        return x + y * size + z*size*size;
    }
    public void addDensity(int x, int y, int z, float amount)
    {
        int index = IX(x, y, z);
        this.density[index] += amount;
    }
    public void addVelocity(int x, int y, int z, float amountX, float amountY, float amountZ)
    {
        int index = IX(x, y, z);
        this.Vx[index] += amountX;
        this.Vy[index] += amountY;
        this.Vz[index] += amountZ;
    }
    public void diffuse(int b, float[] x, float[] x0, float diff, float dt, int iter, int N)
    {
        float a = dt * diff*(N - 2) * (N-2);
        lin_solve(b, x, x0, a, 1 + 6 * a, iter, N);
    }
    public void project(float [] velocX, float[] velocY, float[] velocZ, float[] p, float[] div, int iter, int N)
    {
        for (int k = 1; k < N - 1; k++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    div[IX(i, j, k)] = -0.5f * (
                             velocX[IX(i + 1, j, k)]
                            - velocX[IX(i - 1, j, k)]
                            + velocY[IX(i, j + 1, k)]
                            - velocY[IX(i, j - 1, k)]
                            + velocZ[IX(i, j, k + 1)]
                            - velocZ[IX(i, j, k - 1)]
                        ) / N;
                    p[IX(i, j, k)] = 0;
                }
            }
        }
        set_bnd(0, div, N);
        set_bnd(0, p, N);
        lin_solve(0, p, div, 1, 6, iter, N);

        for (int k = 1; k < N - 1; k++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    velocX[IX(i, j, k)] -= 0.5f * (p[IX(i + 1, j, k)]
                                                    - p[IX(i - 1, j, k)]) * N;
                    velocY[IX(i, j, k)] -= 0.5f * (p[IX(i, j + 1, k)]
                                                    - p[IX(i, j - 1, k)]) * N;
                    velocZ[IX(i, j, k)] -= 0.5f * (p[IX(i, j, k + 1)]
                                                    - p[IX(i, j, k - 1)]) * N;
                }
            }
        }
        set_bnd(1, velocX, N);
        set_bnd(2, velocY, N);
        set_bnd(3, velocZ, N);
    }
    public void advect(int b, float[] d, float[] d0, float[] velocX, float[] velocY, float[] velocZ, float dt, int N)
    {
        float i0, i1, j0, j1, k0, k1;

        float dtx = dt * (N - 2);
        float dty = dt * (N - 2);
        float dtz = dt * (N - 2);

        float s0, s1, t0, t1, u0, u1;
        float tmp1, tmp2, tmp3, x, y, z;

        float Nfloat = N;
        float ifloat, jfloat, kfloat;
        int i, j, k;

        for (k = 1, kfloat = 1; k < N - 1; k++, kfloat++)
        {
            for (j = 1, jfloat = 1; j < N - 1; j++, jfloat++)
            {
                for (i = 1, ifloat = 1; i < N - 1; i++, ifloat++)
                {
                    tmp1 = dtx * velocX[IX(i, j, k)];
                    tmp2 = dty * velocY[IX(i, j, k)];
                    tmp3 = dtz * velocZ[IX(i, j, k)];
                    x = ifloat - tmp1;
                    y = jfloat - tmp2;
                    z = kfloat - tmp3;

                    if (x < 0.5f) x = 0.5f;
                    if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
                    i0 = Mathf.Floor(x);
                    i1 = i0 + 1.0f;
                    if (y < 0.5f) y = 0.5f;
                    if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
                    j0 = Mathf.Floor(y);
                    j1 = j0 + 1.0f;
                    if (z < 0.5f) z = 0.5f;
                    if (z > Nfloat + 0.5f) z = Nfloat + 0.5f;
                    k0 = Mathf.Floor(z);
                    k1 = k0 + 1.0f;

                    s1 = x - i0;
                    s0 = 1.0f - s1;
                    t1 = y - j0;
                    t0 = 1.0f - t1;
                    u1 = z - k0;
                    u0 = 1.0f - u1;

                    int i0i = Mathf.RoundToInt(i0);
                    int i1i = Mathf.RoundToInt(i1);
                    int j0i = Mathf.RoundToInt(j0);
                    int j1i = Mathf.RoundToInt(j1);
                    int k0i = Mathf.RoundToInt(k0);
                    int k1i = Mathf.RoundToInt(k1);

                    d[IX(i, j, k)] =

                        s0 * (t0 * (u0 * d0[IX(i0i, j0i, k0i)]
                                    + u1 * d0[IX(i0i, j0i, k1i)])
                            + (t1 * (u0 * d0[IX(i0i, j1i, k0i)]
                                    + u1 * d0[IX(i0i, j1i, k1i)])))
                       + s1 * (t0 * (u0 * d0[IX(i1i, j0i, k0i)]
                                    + u1 * d0[IX(i1i, j0i, k1i)])
                            + (t1 * (u0 * d0[IX(i1i, j1i, k0i)]
                                    + u1 * d0[IX(i1i, j1i, k1i)])));
                }
            }
        }
        set_bnd(b, d, N);
    }
    void lin_solve(int b, float[] x, float[] x0, float a, float c, int iter, int N)
    {
        float cRecip = 1.0f / c;
        for (int k = 0; k < iter; k++)
        {
            for (int m = 1; m < N - 1; m++)
            {
                for (int j = 1; j < N - 1; j++)
                {
                    for (int i = 1; i < N - 1; i++)
                    {
                        try { 
                        x[IX(i, j, m)] =
                            (x0[IX(i, j, m)]
                                + a * (x[IX(i + 1, j, m)]
                                        + x[IX(i - 1, j, m)]
                                        + x[IX(i, j + 1, m)]
                                        + x[IX(i, j - 1, m)]
                                        + x[IX(i, j, m + 1)]
                                        + x[IX(i, j, m - 1)]
                               )) * cRecip;
                        }catch(Exception e)
                        {
                            Debug.Log(IX(i,j,m));
                            Debug.Log(x.Length);
                            Debug.Log(x0.Length);
                            throw (e);
                        }
                    }
                }
            }
            set_bnd(b, x, N);
        }
    }
    void set_bnd(int b, float[] x, int N)
    {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    x[IX(i, j, 0)] = b == 3 ? -x[IX(i, j, 1)] : x[IX(i, j, 1)];
                    x[IX(i, j, N - 1)] = b == 3 ? -x[IX(i, j, N - 2)] : x[IX(i, j, N - 2)];
                }
            }
            for (int k = 1; k < N - 1; k++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    x[IX(i, 0, k)] = b == 2 ? -x[IX(i, 1, k)] : x[IX(i, 1, k)];
                    x[IX(i, N - 1, k)] = b == 2 ? -x[IX(i, N - 2, k)] : x[IX(i, N - 2, k)];
                }
            }
            for (int k = 1; k < N - 1; k++)
            {
                for (int j = 1; j < N - 1; j++)
                {
                    x[IX(0, j, k)] = b == 1 ? -x[IX(1, j, k)] : x[IX(1, j, k)];
                    x[IX(N - 1, j, k)] = b == 1 ? -x[IX(N - 2, j, k)] : x[IX(N - 2, j, k)];
                }
            }

            x[IX(0, 0, 0)] = 0.33f * (x[IX(1, 0, 0)]
                                          + x[IX(0, 1, 0)]
                                          + x[IX(0, 0, 1)]);
            x[IX(0, N - 1, 0)] = 0.33f * (x[IX(1, N - 1, 0)]
                                          + x[IX(0, N - 2, 0)]
                                          + x[IX(0, N - 1, 1)]);
            x[IX(0, 0, N - 1)] = 0.33f * (x[IX(1, 0, N - 1)]
                                          + x[IX(0, 1, N - 1)]
                                          + x[IX(0, 0, N-1)]);
            x[IX(0, N - 1, N - 1)] = 0.33f * (x[IX(1, N - 1, N - 1)]
                                          + x[IX(0, N - 2, N - 1)]
                                          + x[IX(0, N - 1, N - 2)]);
            x[IX(N - 1, 0, 0)] = 0.33f * (x[IX(N - 2, 0, 0)]
                                          + x[IX(N - 1, 1, 0)]
                                          + x[IX(N - 1, 0, 1)]);
            x[IX(N - 1, N - 1, 0)] = 0.33f * (x[IX(N - 2, N - 1, 0)]
                                          + x[IX(N - 1, N - 2, 0)]
                                          + x[IX(N - 1, N - 1, 1)]);
            x[IX(N - 1, 0, N - 1)] = 0.33f * (x[IX(N - 2, 0, N - 1)]
                                          + x[IX(N - 1, 1, N - 1)]
                                          + x[IX(N - 1, 0, N - 2)]);
            x[IX(N - 1, N - 1, N - 1)] = 0.33f * (x[IX(N - 2, N - 1, N - 1)]
                                          + x[IX(N - 1, N - 2, N - 1)]
                                          + x[IX(N - 1, N - 1, N - 2)]);
        }
    }

