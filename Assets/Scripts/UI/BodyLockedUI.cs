using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyLockedUI : MonoBehaviour
{
    private Camera _player;

    [Header("Lock Behaviour Parameters")]
    [SerializeField] private float MAX_DIST_X = 0f;
    [SerializeField] private float MAX_DIST_Y = 1f;
    [SerializeField] private float MAX_DIST_Z = 0.375f;
    [SerializeField] private float lerpSpeed = 2f;

    private Vector3 offsetFromPlayer;

    public void Start()
    {
        _player = FindObjectOfType<Camera>();
        if (_player != null)
        {
            offsetFromPlayer = transform.position - _player.transform.position;
        }
    }

    public void Update()
    {
        if (_player == null) return;

        Vector3 currentOffset = transform.position - _player.transform.position;

        bool needsCorrection = 
            Mathf.Abs(currentOffset.x - offsetFromPlayer.x) > MAX_DIST_X ||
            Mathf.Abs(currentOffset.y - offsetFromPlayer.y) > MAX_DIST_Y ||
            Mathf.Abs(currentOffset.z - offsetFromPlayer.z) > MAX_DIST_Z;

        if (needsCorrection)
        {
            Vector3 targetPosition = _player.transform.position + offsetFromPlayer;
            transform.position     = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        }
    }

}