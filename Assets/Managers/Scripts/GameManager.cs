using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private bool _isRestarting = false;

    private GameObject _player;
    private PlayerRespawn _playerRespawn;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        RebindPlayerInScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RestartLevel(float delay = 0f, bool useCheckpoint = true)
    {
        if (_isRestarting) return;

        _isRestarting = true;

        // We’re not reloading the scene anymore, so we don’t need MarkRespawnPending here.
        // Just schedule the respawn on the current player.
        StartCoroutine(RespawnAfterDelay(delay));
    }

    public void LoadNextLevel()
    {
        // Fresh level: clear checkpoint state
        CheckpointManager.Instance?.ResetCheckpoint();
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Ensure player handle is valid (e.g., after a level change)
        if (_playerRespawn == null) RebindPlayerInScene();

        _playerRespawn?.Respawn();
        _isRestarting = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isRestarting = false;
        RebindPlayerInScene();
    }

    private void RebindPlayerInScene()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            _player = pc.gameObject;
            _playerRespawn = _player.GetComponent<PlayerRespawn>();
        }
        else
        {
            _player = null;
            _playerRespawn = null;
        }
    }
}
