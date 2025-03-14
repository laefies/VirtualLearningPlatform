using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Netcode;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SunInteractable : Interactable
{
    private NetworkVariable<float> _angle = new NetworkVariable<float>(90f,  NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _light = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private LineRenderer _beam;
    
    [SerializeField] private Text _angleText;
    [SerializeField] private Text _lightText;
    [SerializeField] private Slider _lightSlider;
    [SerializeField] private Transform cloudParent;

    public override void PrepareComponents()
    {
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = 0.03f;
        _beam.endWidth   = 0.05f;

        _light.OnValueChanged += (oldValue, newValue) => { 
            _lightSlider.SetValueWithoutNotify(newValue);
            _lightText.text = $"{Mathf.Round(newValue * 100)}%";
            UpdateCloudVisibility();
            UpdateBeam();
        }; 

        _angle.OnValueChanged += (oldValue, newValue) => {
            _angleText.text = $"{newValue}°";
            UpdateBeam();
        };
    }

    public override void UpdateComponents()
    {
        if (IsServer) GetIncidenceAngle();
    }

    private void GetIncidenceAngle()
    {
        MarkerInfo panel = spawnable.GetMarkerInfo();

        Vector3 panelNormal = panel.Pose.rotation * Vector3.up;
        Vector3 sunToPanel = (panel.Pose.position - transform.position).normalized;

        float angleRad = Mathf.Acos(Vector3.Dot(panelNormal, sunToPanel));

        _angle.Value = 180 - Mathf.Round(angleRad * Mathf.Rad2Deg);
    }

    private void UpdateBeam() {
        MarkerInfo panel = spawnable.GetMarkerInfo();

        _beam.enabled = true;
        _beam.SetPosition(0, transform.position);
        _beam.SetPosition(1, panel.Pose.position);

        _beam.endWidth = Mathf.Max(0.05f * (1f - _light.Value), 0.03f);

        Color currentColor = _beam.startColor;
        currentColor.a = 1f * (1f - _light.Value);
        _beam.startColor = currentColor;
    }

    public void SetLightPercentage(float value)
    {
        if (_light.Value == value) return;

        UpdateLightServerRpc(value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateLightServerRpc(float value)
    {
        _light.Value = value;
    }

    private void UpdateCloudVisibility()
    {
        if (cloudParent == null) return;

        int totalClouds    = cloudParent.childCount;
        int cloudsToEnable = Mathf.RoundToInt(totalClouds * _light.Value);

        for (int i = 0; i < totalClouds; i++)
        {
            GameObject cloud = cloudParent.GetChild(i).gameObject;
            if (i < cloudsToEnable)
            {
                if (!cloud.activeSelf)
                {
                    Vector3 topPosition    = cloud.transform.localPosition;
                    Vector3 bottomPosition = topPosition + new Vector3(0, -1.5f, 0); 
                    cloud.transform.localPosition = bottomPosition;
                    cloud.SetActive(true);
                    cloud.transform.DOLocalMove(topPosition, 0.5f);
                }
            }
            else
            {
                if (!DOTween.IsTweening(cloud.transform) && cloud.activeSelf) {
                    Vector3 topPosition    = cloud.transform.localPosition;
                    Vector3 bottomPosition = topPosition + new Vector3(0, -1.5f, 0); 

                    cloud.transform.DOLocalMove(bottomPosition, 0.2f).OnComplete(() =>
                    {
                        cloud.SetActive(false);
                        cloud.transform.localPosition = topPosition;
                    });

                }
            }
        }
    }
}