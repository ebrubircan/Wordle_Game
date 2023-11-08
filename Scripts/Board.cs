using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    private static readonly KeyCode[] SUPPORTED_KEYS = new KeyCode[]
    {
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F,
        KeyCode.G, KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R,
        KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X,
        KeyCode.Y, KeyCode.Z,
    };

    private Row[] rows;

    private int rowIndex;
    private int columnIndex;

    private string[] solutions;
    private string[] validWords;
    private string word;
    private int score = 0;
    private int lastScore = 0;
    private bool needNewWord = true;
    private int bonusUsages = 2;
    private bool isBonusButtonActive = true;
    private int[] rowScores = new int[] { 6, 5, 4, 3, 2, 1 }; // Örnek: İlk satır 6 puan, ikinci satır 5 puan...


    [Header("Tiles")]
    public Tile.State emptyState;
    public Tile.State occupiedState;
    public Tile.State correctState;
    public Tile.State wrongSpotState;
    public Tile.State incorrectState;

    [Header("UI")]
    public GameObject invalidWord;
    public GameObject tryAgainButton;
    public GameObject quitButton;
    public GameObject newWordButton;
    public GameObject gameOverPanel;
    public GameObject bonusButton;
    public GameObject nextLevelButton;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI lastScoreText;
    private bool gameIsOver = false;



    private void Awake()
    {
        rows = GetComponentsInChildren<Row>();
    }

    private void Start()
    {
        
        LoadData();
        SetRandomWord();
        needNewWord = false;
        bonusUsages = 2;

        isBonusButtonActive = true;
        bonusButton.SetActive(isBonusButtonActive);
    }

    private void LoadData()
    {
        TextAsset textFile = Resources.Load("official_wordle_common") as TextAsset;
        solutions = textFile.text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        textFile = Resources.Load("official_wordle_all") as TextAsset;
        validWords = textFile.text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    public void TryAgain()
    {
        ClearBoard();
        needNewWord = false;
        gameIsOver = false;
        bonusUsages = 2;
        isBonusButtonActive = true;
        bonusButton.SetActive(isBonusButtonActive);
        UpdateBonusButtonInteractivity();
        enabled = true;
    }

    public void NewGame()
    {
        ClearBoard();
        SetRandomWord();

        needNewWord = false;

        bonusUsages = 2;
        isBonusButtonActive = true;
        bonusButton.SetActive(isBonusButtonActive);
        UpdateBonusButtonInteractivity();
        gameIsOver = false;

        enabled = true;
    }

    public void Quit()
    {
        Application.Quit();
    }

    private void SetRandomWord()
    {
        word = solutions[UnityEngine.Random.Range(0, solutions.Length)];
        word = word.ToLower().Trim();
        UpdateBonusButtonInteractivity();

    }

    private void UseBonus()
    {
        if (bonusUsages > 0)
        {
            Row currentRow = rows[rowIndex];
            for (int i = 0; i < currentRow.tiles.Length; i++)
            {
                Tile tile = currentRow.tiles[i];
                if (tile.state != correctState)
                {
                    tile.SetLetter(word[i]);
                    tile.SetState(correctState);
                    columnIndex = i + 1;  // Sıradaki harfin yerine geçmek için sütun indeksini güncelle
                    break;
                }
            }

            int randomIndex = UnityEngine.Random.Range(0, word.Length);
            char randomLetter = word[randomIndex];

            currentRow.tiles[randomIndex].SetLetter(randomLetter);
            currentRow.tiles[randomIndex].SetState(correctState);

            columnIndex = randomIndex + 1;  // Sıradaki harfin yerine geçmek için sütun indeksini güncelle

            bonusUsages--;
            isBonusButtonActive = (bonusUsages > 0);
            UpdateBonusButtonInteractivity();
        }
        else
        {
            Debug.Log("Bonus hakkınız kalmadı.");
        }
    }
  

    public void UseBonusButton()
    {
        UseBonus();
    }

    private void Update()
    {
        Row currentRow = rows[rowIndex];

        if (HasWon(currentRow))
        {
            needNewWord = true;
            NextWord();
            ClearBoard();
        }

        if (!gameIsOver)
        {
            if (!nextLevelButton.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    columnIndex = Mathf.Max(columnIndex - 1, 0);
                    currentRow.tiles[columnIndex].SetLetter('\0');
                    currentRow.tiles[columnIndex].SetState(emptyState);

                    invalidWord.SetActive(false);
                }
                else if (columnIndex >= currentRow.tiles.Length)
                {
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        SubmitRow(currentRow);
                    }
                }
                else
                {
                    for (int i = 0; i < SUPPORTED_KEYS.Length; i++)
                    {
                        if (Input.GetKeyDown(SUPPORTED_KEYS[i]))
                        {
                            currentRow.tiles[columnIndex].SetLetter((char)SUPPORTED_KEYS[i]);
                            currentRow.tiles[columnIndex].SetState(occupiedState);

                            columnIndex++;
                            break;
                        }
                    }
                }
            }
            UpdateBonusButtonInteractivity();
        }
    }

    private void SubmitRow(Row row)
    {
        if (gameIsOver)
        {
            return;
        }
        if (!IsValidWord(row.word))
        {
            invalidWord.SetActive(true);
            return;
        }

        string remaining = word;
        int correctCount = 0;

        for (int i = 0; i < row.tiles.Length; i++)
        {
            Tile tile = row.tiles[i];

            if (tile.letter == word[i])
            {
                tile.SetState(correctState);

                remaining = remaining.Remove(0, 1);
                correctCount++;
            }
            else if (!word.Contains(tile.letter))
            {
                tile.SetState(incorrectState);
            }
        }

        for (int i = 0; i < row.tiles.Length; i++)
        {
            Tile tile = row.tiles[i];

            if (tile.state != correctState && tile.state != incorrectState)
            {
                if (remaining.Contains(tile.letter))
                {
                    tile.SetState(wrongSpotState);

                    int index = remaining.IndexOf(tile.letter);
                    if (index >= 0)
                    {
                        remaining = remaining.Remove(index, 1);
                    }

                }
                else
                {
                    tile.SetState(incorrectState);
                }
            }
        }

        if (correctCount == word.Length)
        {
            int rowScore = rowScores[rowIndex]; // Satırın puanını al
            score += rowScore; // Skoru artır

            gameOverPanel.SetActive(false);
            needNewWord = true; // Yeni kelimeye geç
            NextWord();
            ClearBoard();
            rowIndex = 0;
        }
        else
        {
            if (HasWon(row))
            {
                gameIsOver = true;
                enabled = false;
            }
        }

        scoreText.text = "Score: " + score;

        rowIndex++;
        columnIndex = 0;

        if (rowIndex >= rows.Length)
        {
            gameIsOver = true;
            enabled = false;
        }

        UpdateLastScore();
    }


    public void NextLevel()
    {
        // Yeni seviyeye geçiş kodunu burada yazın.

        // Yeni kelimeye geç
        needNewWord = true; // Yeni kelimeye geç
        SetRandomWord();
        bonusUsages = 2;
        isBonusButtonActive = true;
        UpdateBonusButtonInteractivity();

        // Tahtayı sıfırla
        ClearBoard();
        rowIndex = 0;

        // Game Over panelini gizle
        gameOverPanel.SetActive(false);

        // Next Level butonunu gizle
        nextLevelButton.SetActive(false);

        // Oyunun tekrar başladığını işaretle
        gameIsOver = false;
    }


    private void ClearBoard()
    {
        for (int row = 0; row < rows.Length; row++)
        {
            for (int col = 0; col < rows[row].tiles.Length; col++)
            {
                rows[row].tiles[col].SetLetter('\0');
                rows[row].tiles[col].SetState(emptyState);
            }
        }

        rowIndex = 0;
        columnIndex = 0;
    }

    private bool IsValidWord(string word)
    {
        word = CleanWord(word);
        for (int i = 0; i < validWords.Length; i++)
        {
            string cleanValidWord = CleanWord(validWords[i]);
            if (string.Equals(cleanValidWord, word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private string CleanWord(string input)
    {
        return new string(input.Where(c => char.IsLetter(c)).ToArray()).ToLower();
    }

    private bool HasWon(Row row)
    {
        for (int i = 0; i < row.tiles.Length; i++)
        {
            if (row.tiles[i].state != correctState)
            {
                return false;
            }
        }

        gameOverPanel.SetActive(false);
        NextWord();

        return true;
    }


    private void NextWord()
    {
        if (needNewWord)
        {
            SetRandomWord();
            bonusUsages = 2;
            isBonusButtonActive = true; // Bonus butonunu aktif yap
            UpdateBonusButtonInteractivity();
        }

        gameIsOver = false;
        ClearBoard();
        rowIndex = 0;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(true);
        }
    }

    private void UpdateBonusButtonInteractivity()
    {
        if (bonusButton != null)
        {
            bonusButton.GetComponent<Button>().interactable = isBonusButtonActive;
        }
    }



    private void OnEnable()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (tryAgainButton != null)
        {
            tryAgainButton.SetActive(false);
        }
        if (quitButton != null)
        {
            quitButton.SetActive(false);
        }
        if (newWordButton != null)
        {
            newWordButton.SetActive(false);
        }
        if (bonusButton != null)
        {
            bonusButton.SetActive(isBonusButtonActive);
        }
    }

    private void OnDisable()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(gameIsOver);

            if (lastScoreText != null)
            {
                lastScoreText.text = "Last Score: " + lastScore.ToString();
            }
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.SetActive(true);
        }
        if (quitButton != null)
        {
            quitButton.SetActive(true);
        }
        if (newWordButton != null)
        {
            newWordButton.SetActive(true);
        }
    }
    private void UpdateLastScore()
    {
        lastScore = score;
        // Burada gameOverPanel içinde yer alan TextMeshProUGUI bileşenini güncellemelisiniz
        // Örnek olarak:
        if (lastScoreText != null)
        {
            lastScoreText.text = "Last Score: " + lastScore.ToString();
        }
    }


}