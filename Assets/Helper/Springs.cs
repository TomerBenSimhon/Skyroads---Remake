// Springs.cs
// A small, allocation-free helper for damped spring motion in Unity.
// Supports scalar and Vector3 positions/velocities.
//
// Usage (scalar):
// float pos = 0f, vel = 0f;
// Springs.Step(ref pos, ref vel, equilibrium: 10f, deltaTime: Time.deltaTime, angularFrequency: 8f, dampingRatio: 0.6f);
//
// Usage (vector):
// Vector3 pos = Vector3.zero, vel = Vector3.zero;
// Springs.Step(ref pos, ref vel, equilibrium: targetPoint, deltaTime: Time.deltaTime, angularFrequency: 10f, dampingRatio: 1.0f); // critically damped-ish

using UnityEngine;

public static class Springs
{
    /// <summary>
    /// Precomputed motion coefficients for a damped spring step:
    /// newPos = posPosCoef * oldPos + posVelCoef * oldVel
    /// newVel = velPosCoef * oldPos + velVelCoef * oldVel
    /// </summary>
    public readonly struct DampedSpringParams
    {
        public readonly float posPosCoef; // multiplies oldPos (relative to equilibrium) when computing newPos
        public readonly float posVelCoef; // multiplies oldVel when computing newPos
        public readonly float velPosCoef; // multiplies oldPos when computing newVel
        public readonly float velVelCoef; // multiplies oldVel when computing newVel

        public DampedSpringParams(float ppc, float pvc, float vpc, float vvc)
        {
            posPosCoef = ppc; posVelCoef = pvc; velPosCoef = vpc; velVelCoef = vvc;
        }
    }

    private const float kEpsilon = 1e-4f;

    /// <summary>
    /// Compute coefficients for a single time step of a damped spring with the given parameters.
    /// angularFrequency is in radians/sec (e.g., 2π * Hz). dampingRatio: 0=undamped, 1=critical, &gt;1 overdamped.
    /// </summary>
    public static DampedSpringParams Calc(float deltaTime, float angularFrequency, float dampingRatio)
    {
        // normalize/guard inputs
        if (deltaTime <= 0f)
        {
            // Identity (no change).
            return new DampedSpringParams(1f, 0f, 0f, 1f);
        }

        if (angularFrequency < 0f) angularFrequency = 0f;
        if (dampingRatio < 0f)     dampingRatio     = 0f;

        if (angularFrequency < kEpsilon)
        {
            // No oscillation -> identity mapping
            return new DampedSpringParams(1f, 0f, 0f, 1f);
        }

        // Branch by damping regime
        if (dampingRatio > 1f + kEpsilon)
        {
            // Over-damped
            float za = -angularFrequency * dampingRatio;
            float zb = angularFrequency * Mathf.Sqrt(dampingRatio * dampingRatio - 1f);
            float z1 = za - zb; // (more negative)
            float z2 = za + zb; // (less negative)

            float e1 = Mathf.Exp(z1 * deltaTime);
            float e2 = Mathf.Exp(z2 * deltaTime);

            float invTwoZb = 1f / (2f * zb);  // == 1 / (z2 - z1)

            float e1_over_2zb = e1 * invTwoZb;
            float e2_over_2zb = e2 * invTwoZb;

            float z1e1_over_2zb = z1 * e1_over_2zb;
            float z2e2_over_2zb = z2 * e2_over_2zb;

            float posPos =  e1_over_2zb * z2 - z2e2_over_2zb + e2;
            float posVel = -e1_over_2zb     + e2_over_2zb;

            float velPos = (z1e1_over_2zb - z2e2_over_2zb + e2) * z2;
            float velVel = -z1e1_over_2zb + z2e2_over_2zb;

            return new DampedSpringParams(posPos, posVel, velPos, velVel);
        }
        else if (dampingRatio < 1f - kEpsilon)
        {
            // Under-damped (oscillatory)
            float omegaZeta = angularFrequency * dampingRatio;
            float alpha     = angularFrequency * Mathf.Sqrt(1f - dampingRatio * dampingRatio);

            float expTerm = Mathf.Exp(-omegaZeta * deltaTime);
            float cosTerm = Mathf.Cos(alpha * deltaTime);
            float sinTerm = Mathf.Sin(alpha * deltaTime);

            float invAlpha = 1f / alpha;

            float expSin = expTerm * sinTerm;
            float expCos = expTerm * cosTerm;
            float expOmegaZetaSin_over_alpha = expTerm * omegaZeta * sinTerm * invAlpha;

            float posPos = expCos + expOmegaZetaSin_over_alpha;
            float posVel = expSin * invAlpha;

            float velPos = -expSin * alpha - omegaZeta * expOmegaZetaSin_over_alpha;
            float velVel =  expCos - expOmegaZetaSin_over_alpha;

            return new DampedSpringParams(posPos, posVel, velPos, velVel);
        }
        else
        {
            // Critically damped
            float expTerm     = Mathf.Exp(-angularFrequency * deltaTime);
            float timeExp     = deltaTime * expTerm;
            float timeExpFreq = timeExp * angularFrequency;

            float posPos = timeExpFreq + expTerm;
            float posVel = timeExp;

            float velPos = -angularFrequency * timeExpFreq;
            float velVel = -timeExpFreq + expTerm;

            return new DampedSpringParams(posPos, posVel, velPos, velVel);
        }
    }

    // ---------- Scalar (float) API ----------

    /// <summary>
    /// Advance position and velocity by one step using precomputed coefficients.
    /// </summary>
    public static void Update(ref float pos, ref float vel, float equilibrium, in DampedSpringParams p)
    {
        float oldPos = pos - equilibrium; // work in equilibrium-relative space
        float oldVel = vel;

        pos = oldPos * p.posPosCoef + oldVel * p.posVelCoef + equilibrium;
        vel = oldPos * p.velPosCoef + oldVel * p.velVelCoef;

        // NaN guard (can happen with extreme inputs)
        if (!IsFinite(pos)) pos = equilibrium;
        if (!IsFinite(vel)) vel = 0f;
    }

    /// <summary>
    /// Convenience: compute coefficients and step in a single call.
    /// </summary>
    public static void Step(ref float pos, ref float vel, float equilibrium, float deltaTime, float angularFrequency, float dampingRatio)
    {
        var p = Calc(deltaTime, angularFrequency, dampingRatio);
        Update(ref pos, ref vel, equilibrium, p);
    }

    // ---------- Vector3 API ----------

    /// <summary>
    /// Advance Vector3 position and velocity using precomputed coefficients (applies independently per component).
    /// </summary>
    public static void Update(ref Vector3 pos, ref Vector3 vel, Vector3 equilibrium, in DampedSpringParams p)
    {
        // X
        float px = pos.x, vx = vel.x;
        Update(ref px, ref vx, equilibrium.x, p);
        // Y
        float py = pos.y, vy = vel.y;
        Update(ref py, ref vy, equilibrium.y, p);
        // Z
        float pz = pos.z, vz = vel.z;
        Update(ref pz, ref vz, equilibrium.z, p);

        pos = new Vector3(px, py, pz);
        vel = new Vector3(vx, vy, vz);
    }

    /// <summary>
    /// Convenience: compute coefficients and step Vector3 in a single call.
    /// </summary>
    public static void Step(ref Vector3 pos, ref Vector3 vel, Vector3 equilibrium, float deltaTime, float angularFrequency, float dampingRatio)
    {
        var p = Calc(deltaTime, angularFrequency, dampingRatio);
        Update(ref pos, ref vel, equilibrium, p);
    }

    // ---------- Helpers ----------

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
}
