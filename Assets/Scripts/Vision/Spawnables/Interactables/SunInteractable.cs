using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Netcode;
using Unity.Mathematics;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshRenderer))]
public class SunInteractable : Interactable
{
    private static float _panelEffic = 0.18f; // 18%
    private static float _panelArea  = 2f;    // m2

    private static float MIN_POWER = 0f;
    private static float MAX_POWER = 800f * _panelArea * _panelEffic;
    private static float BEAM_START_WIDTH = 0.07f; // 0.03f
    private static float BEAM_END_WIDTH = 0.1f;  // 0.05f

    private NetworkVariable<float> _angle = new NetworkVariable<float>(90f,  NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _light = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _power = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private LineRenderer _beam;
    [SerializeField] private Text _angleText;

    [SerializeField] private Text _powerText;
    [SerializeField] private Transform _powerPointer;

    [SerializeField] private Text _lightText;
    [SerializeField] private Slider _lightSlider;
    [SerializeField] private Transform cloudParent;

    // Inherited methods - component preparation and updating
    public override void PrepareComponents()
    {
        _beam = GetComponent<LineRenderer>();
        _beam.startWidth = BEAM_START_WIDTH;
        _beam.endWidth   = BEAM_END_WIDTH;

        _light.OnValueChanged += (oldValue, newValue) => { 
            _lightSlider.SetValueWithoutNotify(newValue);
            _lightText.text = $"{ Mathf.Round(newValue * 100) }%";
            UpdateCloudVisibility();
        }; 

        _angle.OnValueChanged += (oldValue, newValue) => {
            _angleText.text = (newValue <= 90) ? $"{ newValue }°" : "-";
        };

        _power.OnValueChanged += (oldValue, newValue) => {
            float power = Mathf.Max(MIN_POWER, newValue);
            _powerText.text = $"{ Mathf.Round( power * 10f ) / 10f }W";

            float pointerRotation = math.remap( MIN_POWER, MAX_POWER, 120, -120, power );
            _powerPointer.localRotation = Quaternion.Euler(0, 0, pointerRotation);
        };
    }

    public override void UpdateComponents()
    {
        if (IsServer) { GetIncidenceAngle();
                   UpdateSolarPowerOutput(); }
                   
        UpdateBeam();
    }

    private void UpdateBeam() {
        Transform panel = spawnable.transform;

        _beam.enabled = _angle.Value <= 90;
        _beam.SetPosition(0, transform.position);
        _beam.SetPosition(1, panel.position);

        _beam.endWidth = Mathf.Max(BEAM_END_WIDTH * (1f - _light.Value), BEAM_START_WIDTH);

        Color currentColor = _beam.startColor;
        currentColor.a = 1f * (1f - _light.Value);
        _beam.startColor = currentColor;
    }

    // Method to calculate power output
    private void UpdateSolarPowerOutput() {
        float dirNormIrr = 800f * Mathf.Exp(-3f * _light.Value); 
        float incFactor  = Mathf.Cos(_angle.Value * Mathf.Deg2Rad);

        _power.Value = dirNormIrr * incFactor * _panelArea * _panelEffic; 
    }

    // Methods relating to incidence angle
    private void GetIncidenceAngle()
    {
      
        /* The goal is to achieve:
        *   Sun directly above solar panel                → 0°
        *   Sun leveled with the panel ("on the horizon") → 90°
        *   Sun anywhere "above horizon"                  → ]0°,  90°[
        *   Sun anywhere "under horizon"                  → ]0°, -90°[  */

        // Direction vector from panel to sun, in the panel's local space
        Transform panel = spawnable.transform;
        Vector3 sunDirection = panel.InverseTransformDirection((transform.position - panel.position).normalized);

        //                                       v Calculates elevation angle from horizontal
        _angle.Value = Mathf.Round(90f - Mathf.Asin(sunDirection.y) * Mathf.Rad2Deg);
        //                                                                  ^ converts from radians to degrees
    }

    // Methods relating to light percentage / cloud visibility
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