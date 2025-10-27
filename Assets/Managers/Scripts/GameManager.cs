using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DefaultExecutionOrder(-11)]
public class GameManager : MonoBehaviour, MenuInput.IMenuActions
{
    public static GameManager Instance { get; private set; }
    private bool _isRestarting = false;
    public bool isPaused = false;
    
    [SerializeField] GameObject _pauseMenu;

    private GameObject _player;
    private PlayerRespawn _playerRespawn;
    private PlayerDeath _playerDeath;
    
    private MenuInput _input; // auto-generated C# class from your input actions
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        _input = new MenuInput();
        _input.Menu.SetCallbacks(this);   // hook this class
        _input.Menu.Enable();     

        RebindPlayerInScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _input?.Dispose();
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
        int index = SceneManager.GetActiveScene().buildIndex;
        
        if (index < SceneManager.sceneCountInBuildSettings - 1)
            SceneManager.LoadScene(index + 1);
    }

    public void LoadPreviousLevel()
    {
        CheckpointManager.Instance?.ResetCheckpoint();
        int index = SceneManager.GetActiveScene().buildIndex;
        if(index > 1)
            SceneManager.LoadScene(index - 1);
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
        ShowCursor(SceneManager.GetActiveScene().buildIndex == 0);
        GlobalEvents.Raise(GlobalEvents.Id.OnSceneStarted);
    }
    
    public void OnPause(InputAction.CallbackContext context)
    {
        if(context.performed) PauseGame();
    }

    private void RebindPlayerInScene()
    {
        var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (pc != null)
        {
            _player = pc.gameObject;
            _playerRespawn = _player.GetComponent<PlayerRespawn>();
            _playerDeath = _player.GetComponent<PlayerDeath>();
        }
        else
        {
            _player = null;
            _playerRespawn = null;
            _playerDeath = null;
        }
    }

    public void KillPlayer()
    {
        PauseGame();
        if(!_player.activeSelf) return;
        _playerDeath?.Die();
    }

    public void PauseGame()
    {
        isPaused = !isPaused; // toggle
        
        _pauseMenu?.SetActive(isPaused);
        AudioListener.pause = isPaused;
        
        if(SceneManager.GetActiveScene().buildIndex != 0)
            ShowCursor(isPaused);
        
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ShowCursor(bool show)
    {
        Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = show;
    }


    
}
