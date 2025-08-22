using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class StickDirectionAnalyzer
{
    private readonly Queue<(Vector2 input, float timeStamp)> _inputBuffer = new();
    private readonly int _maxBufferSize = 30; // approx. 0.5 sec at 60 FPS
    private readonly float _angleThreshold = 30f;
    private float _cooldown = 0.3f;
    private float _lastRotationTime = -Mathf.Infinity;


    /// <summary>
    /// Left right direction
    /// </summary>
    private DirectTurnState _turnState = DirectTurnState.Idle;
    private float _directTurnStartTime;
    private RotationCW _detectedDirection;
    private float _directTurnTimeout = 0.5f; // how long to wait before resetting
    ///

    public enum RotationCW
    {
        CW,
        CCW,
        NONE
    }
    private enum DirectTurnState
    {
        Idle,
        DetectedInput,
        ReturnToCenter
    }
    public RotationCW AnalyzeInput(Vector2 currentInput)
    {
        float now = Time.time;
        currentInput.Normalize();
        if (currentInput.magnitude < 0.1f)
            return RotationCW.NONE;

        _inputBuffer.Enqueue((currentInput, now));

        if (_inputBuffer.Count > _maxBufferSize)
            _inputBuffer.Dequeue();

        float totalAngle = 0f;
        (Vector2 input, float timeStamp)[] inputs = _inputBuffer.ToArray();
        for (int i = 1; i < inputs.Length; i++)
        {
            float angle = Vector2.SignedAngle(inputs[i - 1].input, inputs[i].input);
            totalAngle += angle;
        }

        if (Time.time - _lastRotationTime < _cooldown)
            return RotationCW.NONE;

        if (Mathf.Abs(totalAngle) >= _angleThreshold)
        {
            _inputBuffer.Clear(); // Optional reset to prevent double triggers
            _lastRotationTime = Time.time;
            return totalAngle > 0 ? RotationCW.CCW : RotationCW.CW;
        }

        return RotationCW.NONE;
    }
    public RotationCW CheckDirectInputTurn(Vector2 currentInput, Vector2 playerFacing)
    {
        const float minInputThreshold = 0.3f; // Minimum magnitude of input to be considered valid
        const float angleTolerance = 30f;     // Tolerance for detecting left/right angles

        // Only proceed if the input magnitude is large enough
        if (currentInput.magnitude < minInputThreshold)
        {
            return RotationCW.NONE;  // Ignore small inputs (e.g., jitter or very small stick movement)
        }

        // Normalize the input vectors after ensuring they are significant enough
        currentInput.Normalize();
        playerFacing.Normalize();

        // Calculate the angle between the player's facing direction and the input direction
        float angle = Vector2.SignedAngle(playerFacing, currentInput);

        Debug.Log("Current Angle: " + angle);

        // If the turn state is idle, process the new input
        if (_turnState == DirectTurnState.Idle)
        {
            Debug.Log("Detecting input if (_turnState == DirectTurnState.Idle) is true");

            // If the angle is close to 90 or -90 degrees, we consider it as a CW or CCW turn
            if (angle > 90f - angleTolerance && angle < 90f + angleTolerance)
            {
                _turnState = DirectTurnState.DetectedInput;
                _directTurnStartTime = Time.time;
                _detectedDirection = RotationCW.CCW;  // Player is turning CCW
                Debug.Log("Detected CCW input");
            }
            else if (angle < -90f + angleTolerance && angle > -90f - angleTolerance)
            {
                _turnState = DirectTurnState.DetectedInput;
                _directTurnStartTime = Time.time;
                _detectedDirection = RotationCW.CW;  // Player is turning CW
                Debug.Log("Detected CW input");
            }
        }
        // If the stick is returned to neutral (low magnitude), the turn state is returned to idle
        else if (_turnState == DirectTurnState.DetectedInput)
        {
            _turnState = DirectTurnState.Idle;
            Debug.Log("Returning to idle state");
        }

        // Return the detected rotation direction if any
        if (_turnState == DirectTurnState.DetectedInput)
        {
            return _detectedDirection;
        }

        // If no valid turn detected, return NONE
        return RotationCW.NONE;
    }
}
