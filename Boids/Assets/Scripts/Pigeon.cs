using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pigeon : MonoBehaviour
{
    // Convert meters to centimeters
    public float bodyScale = 0.01F;

    // Thickness of feathrers/lines
    public float lineThickness = 0.2f;

    // Frequency of wing flap
    public float flapFrequency = 1.0F;

    // Frequency of tail flap
    public float tailFrequency = 1.0F;
    public Material material;
    Vector3 lShoulder, lElbow, lWrist, lHand;
    Vector3[][] lSecondaryFeather = new Vector3[8][];
    Vector3[] lPrimaryFeather = new Vector3[10];
    Vector3 rShoulder, rElbow, rWrist, rHand;
    Vector3[][] rSecondaryFeather = new Vector3[8][];
    Vector3[] rPrimaryFeather = new Vector3[10];
    Vector3 tail;

    Vector3 []tailFan = new Vector3[12];

    float DOF = 0;
    float tailSpread = 0;

    float Neck2Shoulder, Shoulder2Elbow, Elbow2Wrist, Wrist2Hand, TailLength, SecondaryFeather;

    Vector3 bodyRotation(Vector3 vec, Vector3 axis, float angle) {
        return Quaternion.AngleAxis(angle / Mathf.PI * 180, axis) * vec;
    }

    Vector3 calcElbow(float DOF, Vector3 neck, Vector3 shoulder, Vector3 tail, bool isLeft) {
        var axisx = Vector3.Normalize(shoulder - neck);
        var axisy = Vector3.Normalize(Vector3.Cross(shoulder - neck, tail - neck));
        if (isLeft) axisy = -axisy;
        var axisz = Vector3.Normalize(Vector3.Cross(axisx, axisy));
        if (!isLeft) axisz = Vector3.Normalize(Vector3.Cross(axisy, axisx));


        var rx = (Mathf.Cos((DOF + Mathf.PI * 1.33F)) + 1.1F) / 2F;
        var ry = (Mathf.Cos((DOF + Mathf.PI * 1.33F)) + 0.8F) / 2F;
        var rz = Mathf.Sin(DOF / 2) * Mathf.Sin(DOF / 4);

        var coord = (Quaternion.AngleAxis(ry, axisy) * Quaternion.AngleAxis(rx, axisx) * Quaternion.AngleAxis(rz, axisz)).ToEuler();

        coord = Vector3.Normalize(coord) * Shoulder2Elbow;

        return shoulder + coord;
    }

    Vector3 calcWrist(float DOF, Vector3 shoulder, Vector3 tail, Vector3 elbow, bool isLeft) {
        var axisx = Vector3.Normalize(elbow - shoulder);
        var axisy = Vector3.Normalize(Vector3.Cross(shoulder, tail));
        if (!isLeft) axisy = Vector3.Normalize(Vector3.Cross(tail, shoulder));


        var rx = Mathf.Sin((DOF - Mathf.PI / 2)) * Mathf.PI / 4F + Mathf.PI * 0.85F / 2;
        var ry = Mathf.Cos(DOF) * Mathf.PI / 1.8F + Mathf.PI / 2F / 2;

        var coord = (Quaternion.AngleAxis(rx, axisx) * Quaternion.AngleAxis(ry, axisy)).ToEuler();
        coord = Vector3.Normalize(coord) * Elbow2Wrist;

        return elbow + coord;
    }

    Vector3 calcHand(float DOF, Vector3 elbow, Vector3 wrist) {
        var coord = Vector3.Normalize(wrist - elbow);
        var rx = Mathf.Sin(DOF) * Mathf.PI / 4 + Mathf.PI / 4;
        var axisy = Vector3.Normalize(Vector3.Cross(wrist, elbow));
        coord = Vector3.Normalize(Quaternion.AngleAxis(rx, coord).ToEuler()) * Wrist2Hand;
        return wrist + coord;
    }

    Vector3 Mirror(Vector3 a, Vector3 norm) {
        return a - 2 * Vector3.ProjectOnPlane(a, norm);
    }


    void randomLeft() {

        this.tail = transform.localPosition - new Vector3(0, 0, Neck2Shoulder);

        this.lShoulder = transform.localPosition + Vector3.Normalize(new Vector3(-1, 0, 1.7F)) * Neck2Shoulder / 2.0F;
        this.lElbow = this.lShoulder + Vector3.Normalize(new Vector3(-1, 0, -1)) * Shoulder2Elbow;
        this.lWrist = this.lElbow + Vector3.Normalize(new Vector3(-1.7F, 0, 1)) * Elbow2Wrist;
        this.lHand = this.lWrist + Vector3.Normalize(new Vector3(-1, 0, -1)) * Wrist2Hand;

        this.rShoulder = transform.localPosition + Vector3.Normalize(new Vector3(1, 0, 1.7F)) * Neck2Shoulder / 2.0F;
        this.rElbow = this.rShoulder + Vector3.Normalize(new Vector3(1, 0, -1)) * Shoulder2Elbow;
        this.rWrist = this.rElbow + Vector3.Normalize(new Vector3(1.7F, 0, 1)) * Elbow2Wrist;
        this.rHand = this.rWrist + Vector3.Normalize(new Vector3(1, 0, -1)) * Wrist2Hand;

        // Use flapFrequency to change the speed of wing flapping
        DOF += 0.01F * flapFrequency;
        if (DOF > 6.28) DOF = 0F;

        lElbow = calcElbow(DOF, transform.localPosition, lShoulder, tail, false);
        rElbow = calcElbow(DOF, transform.localPosition, rShoulder, tail, true);

        lWrist = calcWrist(DOF, lShoulder, tail, lElbow, false);
        rWrist = calcWrist(DOF, rShoulder, tail, rElbow, true);

        lHand = calcHand(DOF, lElbow, lWrist);
        lHand = calcHand(DOF, rElbow, rWrist);

        {
            var coord = Vector3.Normalize(lWrist - lElbow);
            var rx = Mathf.Sin(DOF) * Mathf.PI / 4 + Mathf.PI / 4 + 0.01F;
            var axisy = Vector3.Normalize(Vector3.Cross(lWrist, lElbow));
            coord = Vector3.Normalize(Quaternion.AngleAxis(rx, coord).ToEuler()) * Wrist2Hand;
            this.lHand = lWrist + coord;
        }
        {
            var coord = Vector3.Normalize(rWrist - rElbow);
            var rx = Mathf.Sin(DOF) * Mathf.PI / 4 + Mathf.PI / 4 + 0.01F;
            var axisy = Vector3.Normalize(Vector3.Cross(rWrist, rElbow));
            coord = Vector3.Normalize(Quaternion.AngleAxis(rx, coord).ToEuler()) * Wrist2Hand;
            this.rHand = rWrist + coord;
        }

    }

    void calcTailFan() {
        Vector3 norm = Vector3.Normalize(Vector3.Cross(this.lShoulder - transform.localPosition, tail - transform.localPosition));
        Vector3 x = Vector3.Normalize(Vector3.Cross(tail, norm));
        Vector3 y = Vector3.Normalize(tail - transform.localPosition);
        float angle = (Mathf.PI / 3.0F) * (1F + Mathf.Cos(tailSpread) / 10.0F);
        float delta = (Mathf.PI - angle * 2) / 12F;
        for (int i = 0; i < 12; ++i) {

            // Adjust last sine function to alter flap frequency
            tailFan[i] = tail + Vector3.Normalize(x * Mathf.Cos(angle) + y * Mathf.Sin(angle) + norm * Mathf.Sin(tailFrequency * tailSpread) / 5) * TailLength;
            angle += delta;
        }
        tailSpread += 0.005F;
    }

    private void buildSecondaryFeatherImpl(Vector3 elbow, Vector3 wrist, Vector3[][] feather) {
        var dy = Vector3.Normalize(tail - transform.localPosition);
        var armDir = wrist - elbow;
        for (int i = 0; i < 8; ++i) {
            feather[i][0] = elbow + armDir / 8F * i;
            feather[i][1] = feather[i][0] + Vector3.Normalize(dy + armDir / 5 * Mathf.Sin(flapFrequency * Mathf.PI / 20 * (i - 3.5F))) * SecondaryFeather;
        }
    }

    void buildPrimaryFeatherImpl(Vector3 hand, Vector3 wrist, Vector3 elbow, Vector3 []primary, bool shouldNeg) {
        var norm = Vector3.Cross(hand, wrist);
        norm = Vector3.Cross(norm, hand);
        var wing = Vector3.Normalize(hand - wrist);
        for (int i = 0; i < 10; ++i) {
             primary[i] = hand + (wrist - hand) * i * 0.1F + wing * (-0.5F * i + 16F) * bodyScale;
             wing = bodyRotation(wing, norm, Mathf.PI / 18 * (shouldNeg ? -1 : 1));
        }
    }
    public void buildFeather() {
        buildPrimaryFeatherImpl(lHand, lWrist, lElbow, lPrimaryFeather, false);
        buildPrimaryFeatherImpl(rHand, rWrist, lElbow, rPrimaryFeather, true);
        buildSecondaryFeatherImpl(lElbow, lWrist, lSecondaryFeather);
        buildSecondaryFeatherImpl(rElbow, rWrist, rSecondaryFeather);
        calcTailFan();
    }

    public void Awake()
    {
        Neck2Shoulder = 14 * bodyScale;
        Shoulder2Elbow = 2 * bodyScale;
        Elbow2Wrist = 5 * bodyScale;
        Wrist2Hand = 2 * bodyScale;
        TailLength = 10 * bodyScale;
        SecondaryFeather = 11 * bodyScale;
    }

    public void Start() {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<LineRenderer>();
        for (int i = 0; i < 8; ++i) {
            lSecondaryFeather[i] = new Vector3[2];
            rSecondaryFeather[i] = new Vector3[2];
        }
        randomLeft();
        buildFeather();
        buildFeather();
    }

    public void Update() {
        randomLeft();
        buildFeather();
        var mesh = GetComponent<MeshFilter>().mesh;
        var lr = gameObject.GetComponent<LineRenderer>();
        
        // Scale down the feather width
        lr.startWidth = lineThickness;
        lr.endWidth = lineThickness;

        mesh.Clear();

        List<Vector3> a = new List<Vector3>();
        lr.positionCount = 6;
        a.Add(transform.localPosition);
        a.Add(tail);
        for (int i = 0; i < 12; ++i) {
            a.Add(tailFan[i]);
            a.Add(tail);
        }
        a.Add(transform.localPosition);
        a.Add(lShoulder);
        for (int i = 0; i < 8; ++i) {
            a.Add(lSecondaryFeather[i][0]);
            a.Add(lSecondaryFeather[i][1]);
            a.Add(lSecondaryFeather[i][0]);
        }
        a.Add(lElbow);
        a.Add(lWrist);
        for (int i = 0; i < 10; ++i) {
            a.Add(lPrimaryFeather[i]);
            a.Add(lWrist);
        }
        a.Add(lHand);
        a.Add(lWrist);
        a.Add(lElbow);
        a.Add(lShoulder);
        a.Add(transform.localPosition);
        a.Add(rShoulder);
        for (int i = 1; i < 8; ++i) {
            a.Add(rSecondaryFeather[i][0]);
            a.Add(rSecondaryFeather[i][1]);
            a.Add(rSecondaryFeather[i][0]);
        }
        a.Add(rElbow);
        a.Add(rWrist);
        for (int i = 0; i < 10; ++i) {
            a.Add(rPrimaryFeather[i]);
            a.Add(rWrist);
        }
        a.Add(rHand);

        lr.material = material;
        lr.positionCount = a.Count;

        // Convert to world positions
        List<Vector3> final = new List<Vector3>();
        foreach (Vector3 vec in a) {
            final.Add(transform.TransformPoint(vec));
        }

        lr.SetPositions(final.ToArray());

        // var velocity = Vector3.Normalize(Vector3.Cross(Vector3.Cross(lShoulder, rShoulder), tail)) * 0.1F;
    }
}
