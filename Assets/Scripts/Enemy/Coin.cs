using UnityEngine;
using ZombieGame;

public class Coin : MonoBehaviour
{
    private enum State { Idle, Jumping, Chasing }
    private State _currentState = State.Idle;

    public int CoinAmount { get; set; }
    public float magnetRange; // Magnet range for the coin
    private bool isMagnetized = false;
    private float movementSpeed = 7f; // Speed at which the coin moves towards the player
    private GameObject _player;
    AudioClip _coinSound;
    private bool _pickedUp = false;

    private float _jumpTimer = 0f;
    private const float JUMP_DURATION = 0.5f;
    private Vector3 _jumpStartPos;
    private float _chaseAcceleration = 20f;
    private float _currentChaseSpeed = 0f;


    public static Coin Create(Vector3 position, int coinAmount)
    {
        GameObject coinPrefab = Resources.Load<GameObject>("Enemies/Coins/CopperCoin");
        Transform coinTransform = Instantiate(coinPrefab.transform, position, Quaternion.identity);
        Coin coin = coinTransform.GetComponent<Coin>();

        coin.Setup(coinAmount);
        coin.magnetRange = 15f; // Adjusted to a more reasonable range
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
        _player = GameObject.Find("Player");
        transform.Rotate(0, 0, 90);
        _currentChaseSpeed = movementSpeed;
    }

    void Update()
    {
        if (_pickedUp) return;

        // Visual rotation
        transform.Rotate(6, 0, 0);

        switch (_currentState)
        {
            case State.Idle:
                if (isMagnetized && _player != null)
                {
                    StartJump();
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
        _currentState = State.Jumping;
        _jumpTimer = 0f;
        _jumpStartPos = transform.position;
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
        Vector3 targetPos = _player != null ? _player.transform.position : _jumpStartPos;
        Vector3 horizontalPos = Vector3.Lerp(_jumpStartPos, targetPos, t * 0.5f); // Move halfway towards player during jump
        float height = Mathf.Sin(t * Mathf.PI) * 2.5f; // Arc height
        transform.position = horizontalPos + Vector3.up * height;
    }

    private void UpdateChase()
    {
        if (_player == null) return;

        _currentChaseSpeed += _chaseAcceleration * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, _currentChaseSpeed * Time.deltaTime);
        
        // Scale punch effect as it gets closer
        float dist = Vector3.Distance(transform.position, _player.transform.position);
        if (dist < 2f)
        {
            float scale = Mathf.Lerp(1.5f, 1f, dist / 2f);
            transform.localScale = Vector3.one * scale;
        }

        PickUpAndDestroyCoin(_player, gameObject);
    }

    void PickUpAndDestroyCoin(GameObject player, GameObject coin)
    {
        float distance = Vector3.Distance(player.transform.position, coin.transform.position);
        // Increased distance slightly to feel more responsive with high speed
        if (distance <= 0.5f && !_pickedUp)
        {
            if (SoundFXManager.Instance != null)
                SoundFXManager.Instance.PlaySoundFXClip(_coinSound, transform, 1f);
            
            _player.GetComponent<PlayerControllerInput>().AddCoins(CoinAmount);
            _pickedUp = true;
            
            // Pop effect before destruction
            transform.localScale = Vector3.zero;
            Invoke("DestroyCoin", 0.1f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance <= magnetRange)
            {
                isMagnetized = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // We don't disable magnetization once started for better feel
            // isMagnetized = false; 
        }
    }

    private void DestroyCoin()
    {
        Destroy(gameObject);
    }
}
