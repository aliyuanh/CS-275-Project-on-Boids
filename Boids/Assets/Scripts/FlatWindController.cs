using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlatWindController : MonoBehaviour
{
    // Start is called before the first frame update
    int N = 10;
    myFluid myFluidBoi;
    List<GameObject> myVectors;
    void Start()
    {
        myFluidBoi = new myFluid(1f, .01f, .01f);
        myVectors = new List<GameObject>();
        for(int i = 0; i < N; i++)
        {
            for(int j = 0; j < N; j++)
            {
                GameObject myThing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                myThing.transform.position = new Vector3(i, j, 0);
                myThing.transform.localScale = new Vector3(.2f, .2f, .2f);
                myVectors.Add(myThing);
            }
        }
    }

    void FixedUpdate()
    {
        myFluidBoi.addDensity(5, 5, 20f);
        myFluidBoi.step();
        for(int i = 0; i < myVectors.Count; i++)
        {
            float myFluidX = myFluidBoi.Vx[i];
            float myFluidY = myFluidBoi.Vy[i];
            Vector3 rotationVec = new Vector3(myFluidX, myFluidY, 0);
            Debug.Log(rotationVec);
            //myVectors[i].transform.rotation = Quaternion.LookRotation(rotationVec);
        }
    }

    
}
public class myFluid
{
    private int size;
    float dt;
    float diff;
    float visc;

    float[] s;
    float[] density;

    public float[] Vx;
    public float[] Vy;

    float[] Vx0;
    float[] Vy0;
    int N = 10;

    public void step()
    {
        int N = this.size;
        float visc = this.visc;
        float diff = this.diff;
        float dt = this.dt;
        float[] Vx = this.Vx;
        float[] Vy = this.Vy;
        float[] Vx0 = this.Vx0;
        float[] Vy0 = this.Vy0;
        float[] s = this.s;
        float[] density = this.density;

        diffuse(1, Vx0, Vx, visc, dt);
        diffuse(2, Vy0, Vy, visc, dt);

        project(Vx0, Vy0, Vx, Vy);

        advect(1, Vx, Vx0, Vx0, Vy0, dt);
        advect(2, Vy, Vy0, Vx0, Vy0, dt);

        project(Vx, Vy, Vx0, Vy0);

        diffuse(0, s, density, diff, dt);
        advect(0, density, s, Vx, Vy, dt);
    }
    public myFluid(float dt, float diffusion, float viscosity)
    {

        this.size = N;
        this.dt = dt;
        this.diff = diffusion;
        this.visc = viscosity;

        this.s = new float[N * N];
        this.density = new float[N * N];

        this.Vx = new float[N * N];
        this.Vy = new float[N * N];

        this.Vx0 = new float[N * N];
        this.Vy0 = new float[N * N];
    }

    public void addDensity(int x, int y, float amount)
    {
        int index = IX(x, y);
        this.density[index] += amount;
    }

    void addVelocity(int x, int y, float amountX, float amountY)
    {
        int index = IX(x, y);
        this.Vx[index] += amountX;
        this.Vy[index] += amountY;
    }
    void diffuse(int b, float[] x, float[] x0, float diff, float dt)
    {
        float a = dt * diff * (N - 2) * (N - 2);
        lin_solve(b, x, x0, a, 1 + 4 * a);
    }
    int IX(int x, int y)
    {
        if (x > N - 1)
        {
            x = N - 1;
        }
        if (y > N - 1)
        {
            y = N - 1;
        }
        return x + (y * N);
    }
    void lin_solve(int b, float[] x, float[] x0, float a, float c)
    {
        float cRecip = 1.0f / c;
        for (int k = 0; k < N - 1; k++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {
                    x[IX(i, j)] =
                      (x0[IX(i, j)]
                      + a * (x[IX(i + 1, j)]
                      + x[IX(i - 1, j)]
                      + x[IX(i, j + 1)]
                      + x[IX(i, j - 1)]
                      )) * cRecip;
                }
            }

            set_bnd(b, x);
        }
    }
    void project(float[] velocX, float[] velocY, float[] p, float[] div)
    {
        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                div[IX(i, j)] = -0.5f * (
                  velocX[IX(i + 1, j)]
                  - velocX[IX(i - 1, j)]
                  + velocY[IX(i, j + 1)]
                  - velocY[IX(i, j - 1)]
                  ) / N;
                p[IX(i, j)] = 0;
            }
        }

        set_bnd(0, div);
        set_bnd(0, p);
        lin_solve(0, p, div, 1, 4);

        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                velocX[IX(i, j)] -= 0.5f * (p[IX(i + 1, j)]
                  - p[IX(i - 1, j)]) * N;
                velocY[IX(i, j)] -= 0.5f * (p[IX(i, j + 1)]
                  - p[IX(i, j - 1)]) * N;
            }
        }
        set_bnd(1, velocX);
        set_bnd(2, velocY);
    }


    void advect(int b, float[] d, float[] d0, float[] velocX, float[] velocY, float dt)
    {
        float i0, i1, j0, j1;

        float dtx = dt * (N - 2);
        float dty = dt * (N - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float Nfloat = N;
        float ifloat, jfloat;
        int i, j;

        for (j = 1, jfloat = 1; j < N - 1; j++, jfloat++)
        {
            for (i = 1, ifloat = 1; i < N - 1; i++, ifloat++)
            {
                tmp1 = dtx * velocX[IX(i, j)];
                tmp2 = dty * velocY[IX(i, j)];
                x = ifloat - tmp1;
                y = jfloat - tmp2;

                if (x < 0.5f) x = 0.5f;
                if (x > Nfloat + 0.5f) x = Nfloat + 0.5f;
                i0 = Mathf.Floor(x);
                i1 = i0 + 1.0f;
                if (y < 0.5f) y = 0.5f;
                if (y > Nfloat + 0.5f) y = Nfloat + 0.5f;
                j0 = Mathf.Floor(y);
                j1 = j0 + 1.0f;

                s1 = x - i0;
                s0 = 1.0f - s1;
                t1 = y - j0;
                t0 = 1.0f - t1;

                int i0i = (int)i0;
                int i1i = (int)i1;
                int j0i = (int)j0;
                int j1i = (int)j1;

                // DOUBLE CHECK THIS!!!
                d[IX(i, j)] =
                  s0 * (t0 * d0[IX(i0i, j0i)] + t1 * d0[IX(i0i, j1i)]) +
                  s1 * (t0 * d0[IX(i1i, j0i)] + t1 * d0[IX(i1i, j1i)]);
            }
        }

        set_bnd(b, d);
    }



    void set_bnd(int b, float[] x)
    {
        for (int i = 1; i < N - 1; i++)
        {
            x[IX(i, 0)] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
            x[IX(i, N - 1)] = b == 2 ? -x[IX(i, N - 2)] : x[IX(i, N - 2)];
        }
        for (int j = 1; j < N - 1; j++)
        {
            x[IX(0, j)] = b == 1 ? -x[IX(1, j)] : x[IX(1, j)];
            x[IX(N - 1, j)] = b == 1 ? -x[IX(N - 2, j)] : x[IX(N - 2, j)];
        }

        x[IX(0, 0)] = 0.5f * (x[IX(1, 0)] + x[IX(0, 1)]);
        x[IX(0, N - 1)] = 0.5f * (x[IX(1, N - 1)] + x[IX(0, N - 2)]);
        x[IX(N - 1, 0)] = 0.5f * (x[IX(N - 2, 0)] + x[IX(N - 1, 1)]);
        x[IX(N - 1, N - 1)] = 0.5f * (x[IX(N - 2, N - 1)] + x[IX(N - 1, N - 2)]);
    }

}
