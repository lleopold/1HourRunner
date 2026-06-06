using UnityEngine;
using ZombieGame;

public class Coin : MonoBehaviour
{
    private enum State { Idle, Jumping, Chasing }
    private State _currentState = State.Idle;

    public int CoinAmount { get; set; }
    public float magnetRange; // Magnet range for the coin
    private bool isMagnetized = false;
    private float movementSpeed = 7f; // Initial chase speed
    private float _maxSpeed = 15f;    // Target chase speed (will be set to player max speed)
    private GameObject _player;
    AudioClip _coinSound;
    private bool _pickedUp = false;

    private float _jumpTimer = 0f;
    private const float JUMP_DURATION = 0.4f;
    private Vector3 _jumpStartPos;
    private float _chaseAcceleration = 60f;
    private float _currentChaseSpeed = 0f;
    private bool _isInitialized = false;
    private Vector3 _jumpTargetPos;
    private float _distanceCheckTimer = 0f;
    private const float DISTANCE_CHECK_INTERVAL = 0.2f;

    public static Coin Create(Vector3 position, int coinAmount)
    {
        GameObject coinPrefab = Resources.Load<GameObject>("Enemies/Coins/CopperCoin");
        if (coinPrefab == null)
        {
            Debug.LogError("Coin prefab not found at Resources/Enemies/Coins/CopperCoin");
            return null;
        }
        Transform coinTransform = Instantiate(coinPrefab.transform, position, Quaternion.identity);
        Coin coin = coinTransform.GetComponent<Coin>();

        coin.Setup(coinAmount);
        return (coin);
    }

    private void Setup(int coinAmount)
    {
        CoinAmount = coinAmount;
    }

    // Start is called before the first frame update
    void Start()
    {
        _coinSound = Resources.Load<AudioClip>("Enemies/Coins/cashRegister");
        _player = GameObject.FindWithTag("Player");
        transform.Rotate(0, 0, 90);
        
        if (PlayerConfigSingleton.Instance != null && PlayerConfigSingleton.Instance.PlayerConfig != null)
        {
            var pCfg = PlayerConfigSingleton.Instance.PlayerConfig;
            magnetRange = pCfg.PickupDistance;
            
            // Calculate player's max running speed: speed * (1 + RunningSpeed_pct/100)
            float runMultiplier = (pCfg.RunningSpeed_pct / 100f) + 1f;
            _maxSpeed = pCfg.speed * runMultiplier;
            
            // If max speed is very low (e.g. config not set), use a sane default
            if (_maxSpeed < 5f) _maxSpeed = 10f;
        }
        
        _currentChaseSpeed = _maxSpeed * 0.5f; // Start at 50% max speed
        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized || _pickedUp) return;

        // Visual rotation
        transform.Rotate(6, 0, 0);

        if (_player == null)
        {
            _player = GameObject.FindWithTag("Player");
            if (_player == null) return;
        }

        switch (_currentState)
        {
            case State.Idle:
                _distanceCheckTimer -= Time.deltaTime;
                if (_distanceCheckTimer <= 0f)
                {
                    _distanceCheckTimer = DISTANCE_CHECK_INTERVAL;
                    float dist = Vector3.Distance(transform.position, _player.transform.position);
                    if (dist <= magnetRange)
                    {
                        isMagnetized = true;
                        StartJump();
                    }
                }
                break;

            case State.Jumping:
                UpdateJump();
                break;

            case State.Chasing:
                UpdateChase();
                break;
        }
    }

    private void StartJump()
    {
        if (_currentState == State.Jumping) return;
        
        _currentState = State.Jumping;
        _jumpTimer = 0f;
        _jumpStartPos = transform.position;
        
        // Horizontal jump: pop slightly away from the player to create a better "collect" arc
        Vector3 dirFromPlayer = (transform.position - _player.transform.position).normalized;
        if (dirFromPlayer == Vector3.zero) dirFromPlayer = Vector3.forward;
        
        _jumpTargetPos = _jumpStartPos + dirFromPlayer * 1.0f + Random.insideUnitSphere * 0.5f;
        _jumpTargetPos.y = _jumpStartPos.y; 
    }

    private void UpdateJump()
    {
        _jumpTimer += Time.deltaTime;
        float t = _jumpTimer / JUMP_DURATION;

        if (t >= 1f)
        {
            _currentState = State.Chasing;
            return;
        }

        // Parabola logic for jump
        Vector3 horizontalPos = Vector3.Lerp(_jumpStartPos, _jumpTargetPos, t);
        float height = Mathf.Sin(t * Mathf.PI) * 1.5f; 
        transform.position = horizontalPos + Vector3.up * height;
        
        // During jump, we don't check for pickup distance to ensure the "pop" is visible
        // unless the player moves directly into it (handled by OnTriggerEnter)
    }

    private void UpdateChase()
    {
        if (_player == null) return;

        // Accelerate to max speed
        _currentChaseSpeed = Mathf.MoveTowards(_currentChaseSpeed, _maxSpeed, _chaseAcceleration * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _currentChaseSpeed * Time.deltaTime);
        
        // Scale effect as it gets closer
        float dist = Vector3.Distance(transform.position, _player.transform.position);
        if (dist < 2f)
        {
            float scale = Mathf.Lerp(1.5f, 1f, dist / 2f);
            transform.localScale = Vector3.one * scale;
        }

        CheckPickupDistance();
    }

    private void CheckPickupDistance()
    {
        if (_pickedUp || _player == null) return;
        
        float distance = Vector3.Distance(_player.transform.position, transform.position);
        // Distance check for pickup - slightly more generous range for reliability at high speed
        if (distance <= 1.2f)
        {
            HandlePickup();
        }
    }

    void HandlePickup()
    {
        if (_pickedUp) return;
        
        _pickedUp = true;
        
        if (SoundFXManager.Instance != null)
            SoundFXManager.Instance.PlaySoundFXClip(_coinSound, transform, 1f);
        
        PlayerControllerInput playerScript = _player.GetComponent<PlayerControllerInput>();
        if (playerScript == null)
        {
            playerScript = _player.GetComponentInParent<PlayerControllerInput>();
        }

        if (playerScript != null)
        {
            playerScript.AddCoins(CoinAmount);
        }
        
        // Immediate visual disappearance
        transform.localScale = Vector3.zero;
        DestroyCoin();
    }

    private void DestroyCoin()
    {
        Destroy(gameObject);
    }
}
