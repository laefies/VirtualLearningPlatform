using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SunInteractable : Interactable
{
    // TODO » Melhorar raio visualmente;
    private LineRenderer _beam;

    public override void PrepareComponents()
    {
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = 0.01f;
        _beam.endWidth   = 0.03f;
        _beam.startColor = Color.red;
        _beam.endColor   = Color.red;
        _beam.material   = new Material(Shader.Find("Sprites/Default"));
    }

    public override void UpdateComponents()
    {
        MarkerInfo panel = spawnable.GetMarkerInfo();

        _beam.enabled  = true;
        _beam.SetPosition(0, transform.position);
        _beam.SetPosition(1, panel.Pose.position);

        Debug.Log(GetIncidenceAngle(panel));
    }

    public float GetIncidenceAngle(MarkerInfo panel)
    {
        Vector3 panelNormal = panel.Pose.rotation * Vector3.up;
        Vector3 sunToPanel  = (panel.Pose.position - transform.position).normalized;

        float angleRad = Mathf.Acos(Vector3.Dot(panelNormal, sunToPanel));
        return angleRad * Mathf.Rad2Deg;
    }

}