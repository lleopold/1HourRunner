using UnityEngine;

public class Coin : MonoBehaviour
{
    public int CoinAmount { get; set; }
    public float magnetRange; // Magnet range for the coin
    private bool isMagnetized = false;
    private float movementSpeed = 15f; // Speed at which the coin moves towards the player
    private GameObject _player;
    AudioClip _coinSound;
    private bool _pickedUp = false;


    public static Coin Create(Vector3 position, int coinAmount)
    {
        GameObject coinPrefab = Resources.Load<GameObject>("Enemies/Coins/CopperCoin");
        Transform coinTransform = Instantiate(coinPrefab.transform, position, Quaternion.identity);
        Coin coin = coinTransform.GetComponent<Coin>();

        coin.Setup(coinAmount);
        coin.magnetRange = 150f;
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
        transform.position += new Vector3(0, 3, 0);
    }

    void Update()
    {
        transform.Rotate(12, 0, 0);
        if (isMagnetized && _player != null && !_pickedUp)
        {
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, movementSpeed * Time.deltaTime);
            PickUpAndDestroyCoin(_player, gameObject);
        }

    }
    void PickUpAndDestroyCoin(GameObject player, GameObject coin)
    {
        float distance = Vector3.Distance(player.transform.position, coin.transform.position);
        if (distance <= 0.1 && !_pickedUp)
        {
            SoundFXManager.Instance.PlaySoundFXClip(_coinSound, transform, 1f);
            _player.GetComponent<PlayerControllerInput>().AddCoins(CoinAmount);
            _pickedUp = true;
            Invoke("DestroyCoin", 0f);
        }
    }



    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Coin OnTriggerEnter");
        if (other.CompareTag("Player"))
        {
            float distance = Vector3.Distance(transform.position, other.transform.position);
            //Debug.Log("Coin not magnetized, distance: " + distance.ToString());
            if (distance <= magnetRange)
            {
                //Debug.Log("Coin magnetized, distance: " + distance.ToString());
                // Magnetize the coin
                isMagnetized = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Disable magnetization when the player exits the trigger zone
            isMagnetized = false;
        }
    }
    private void DestroyCoin()
    {
        Destroy(gameObject);
    }
}
