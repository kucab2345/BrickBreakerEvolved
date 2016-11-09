﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
	public Player player1, player2;
    public GameObject LinePrefab;
	public GameObject gameOverPanel;
    public GameObject PowerUpPrefab;
	public int maxLineCount;

    public int bricksPerLine = 12;       //bricks needed to send a line.
    public float scorePerBrick = 50f;    //score needed to send a brick.
    public float brickValue = 100f;      //default score awarded per brick hit.

    public float powerUpSpawnRate = 10f; //In seconds.
    private bool isGameOver = false;     //Variable to let Power-Ups know when to stop spawning.

    private float lineSpaceConst = 1.14f;

	private float p1AddLineYOffset = 0;
	private float p2AddLineYOffset = 12;

	private Text player1PendingText;
	private Text player2PendingText;

    private Text player1ScoreText;
    private Text player2ScoreText;

    private Text player1WinsText;
    private Text player2WinsText;

    private Text gameOverText;

    // Use this for initialization
    void Start ()
    {
		Time.timeScale = 1f;
		player1PendingText = GameObject.Find("Player 1 Pending").GetComponent<Text>();
		player2PendingText = GameObject.Find("Player 2 Pending").GetComponent<Text>();

        player1ScoreText = GameObject.Find("Player 1 Score").GetComponent<Text>();
        player2ScoreText = GameObject.Find("Player 2 Score").GetComponent<Text>();

        player1WinsText = GameObject.Find("Player 1 Wins").GetComponent<Text>();
        player2WinsText = GameObject.Find("Player 2 Wins").GetComponent<Text>();

		gameOverText = GameObject.Find("OutcomeText").GetComponent<Text>();

		gameOverPanel.SetActive (false);

        
        player1 = new Player()
        {
            playerNumber = 1,

            wins = 0,

            score = 0,
            comboCount = 0,
			name = "Player 1",

            brickCount = brickCountHelper(GameObject.FindGameObjectWithTag("Bricks1")),
            pendingBricks = 0,

            BrickGroup = GameObject.FindGameObjectWithTag("Bricks1"),
            Paddle = GameObject.FindGameObjectWithTag("Paddle1"),
            Ball = GameObject.FindGameObjectWithTag("Ball1"),

			recentlyAddedLineY = p1AddLineYOffset
        };

        player2 = new Player()
        {
            playerNumber = 2,

            wins = 0,

            score = 0,
            comboCount = 0,
			name = "Player 2",

            brickCount = brickCountHelper(GameObject.FindGameObjectWithTag("Bricks2")),
            pendingBricks = 0,

            BrickGroup = GameObject.FindGameObjectWithTag("Bricks2"),
            Paddle = GameObject.FindGameObjectWithTag("Paddle2"),
            Ball = GameObject.FindGameObjectWithTag("Ball2"),

			recentlyAddedLineY = p2AddLineYOffset
        };

        player1WinsText.text = player1.wins.ToString();
        player2WinsText.text = player2.wins.ToString();

        //Start Dropping Powerups after 5 seconds.
        InvokeRepeating("spawnPowerUp" , 5f , 10f);
    }
    int brickCountHelper(GameObject brickGroup)
    {
        int brickCount = 0;
        for (int i = 0; i < brickGroup.transform.childCount; i++)
        {
            brickCount += brickGroup.transform.GetChild(i).childCount;
        }

        return brickCount;
    }

    // Update is called once per frame
    void Update ()
    {
        //Victory by board clear.
        checkClearVictory(player1, player2);
    }

    //Spawns a powerup to a given player. The disadvantaged player has a better chance of getting the spawned power-up (advantage is based on score).
    void spawnPowerUp()
    {
        //score ratio is the fraction of player1's points to all score earned. scoreRatio > 0.5 means player1 is winning. 
        float scoreRatio = (player1.score + player2.score) == 0 ? 0.5f : player1.score / (player1.score + player2.score);
        //Determines which board will recieve the Power Up
        int playerToRecieve = UnityEngine.Random.value >= scoreRatio ? 1 : 2;

        //Determine Properties for newly instantiated Power Up.
        GameObject paddleToRecieve = GameObject.FindGameObjectWithTag("Paddle" + playerToRecieve);
        int powerUpType = UnityEngine.Random.Range(0, System.Enum.GetNames(typeof(PowerUpKey)).GetLength(0));

        GameObject instantiatedPowerUp = Instantiate(PowerUpPrefab, new Vector3(paddleToRecieve.transform.position.x, 4.5f), Quaternion.identity) as GameObject;
        PowerUp powerUp = instantiatedPowerUp.GetComponent<PowerUp>();

        powerUp.PowerUpPaddle = paddleToRecieve;
        powerUp.powerUpName = (PowerUpKey)powerUpType;
    }

	//End the game, display the GameOver panel, stop time, and display outcome text
	void gameOver(Player winner, Player loser)
	{
		print ("GameOver");		
		gameOverPanel.SetActive(true);
        gameOverText.text = winner.name + " wins!";

        winner.wins = 1;
        player1WinsText.text = winner.wins.ToString();
        player2WinsText.text = loser.wins.ToString();

        CancelInvoke("spawnPowerUp");

        Time.timeScale = 0f;
	}
	
    //Check who has won the game based on number of lines in each player's screen (more conditions to be added)
	void checkClearVictory(Player player1, Player player2)
	{
		if (player1.BrickGroup.transform.childCount <= 0) {
			gameOver(player1, player2);
		}
		if (player2.BrickGroup.transform.childCount <= 0) {
			gameOver(player2, player1);
		}
	}
	void checkLineVictory(Player player1, Player player2)
	{
		//print ("Player1BrickLines :" + player1.BrickGroup.transform.childCount);
		//print ("Player2BrickLines :" + player2.BrickGroup.transform.childCount);

		if (player1.BrickGroup.transform.childCount >= maxLineCount) {
			gameOver(player2, player1);
		}
		if (player2.BrickGroup.transform.childCount >= maxLineCount) {
			gameOver(player1, player2);
		}
	}
    
    // *Messenger Method*
    // Sends bricks to the opponent of 'player' based on the player's current combo. Manages score too.
    void sendBricks(int player)
    {
        Player tempPlayer = player == 1 ? player1 : player2;
        Player victim = player == 2 ? player1 : player2;

        AwardScore(tempPlayer, victim);

		while (tempPlayer.pendingBricks >= bricksPerLine)
		{
            AddLine (tempPlayer, victim);
		}

		player1PendingText.text = "Pending : " + player2.pendingBricks;
		player2PendingText.text = "Pending : " + player1.pendingBricks;

        player1ScoreText.text = "Score : " + player1.score;
        player2ScoreText.text = "Score : " + player2.score;

        //Victory by Line #
		checkLineVictory(player1, player2);
    }

    // *Messenger Method*
    // Resets combo of a player.
    void resetCombo(int player)
    {
        Player tempPlayer = player == 1 ? player1 : player2;

        tempPlayer.comboCount = 0;
    }

    // Awards Score to a player upon breaking a brick.
    // Adds appropriate amount of bricks based on score.
    void AwardScore(Player tempPlayer ,Player tempVictim)
    {
        //Calculate and award score , increment combo count.
        float brickScore = brickValue + (brickValue * tempPlayer.comboCount) / 10;
        tempPlayer.score += brickScore;
        tempPlayer.comboCount++;

        int bricksToSend = (int)(brickScore / scorePerBrick);

        if (tempVictim.pendingBricks > 0)
        {
            int initialVictimPendingBricks = tempVictim.pendingBricks;

            tempVictim.pendingBricks = (tempVictim.pendingBricks - bricksToSend) < 0 ? 0 : tempVictim.pendingBricks - bricksToSend;
            bricksToSend = bricksToSend - initialVictimPendingBricks;
            tempPlayer.pendingBricks = bricksToSend > 0 ? tempPlayer.pendingBricks + bricksToSend : tempPlayer.pendingBricks;
        }
        else
        {
            tempPlayer.pendingBricks += (int)(brickScore / scorePerBrick);
        }

        //print((int)(brickScore / scorePerBrick) + " pending bricks added");

        tempPlayer.brickCount--;
    }

	void AddLine(Player tempPlayer , Player victim)
	{
        tempPlayer.pendingBricks -= bricksPerLine;

        Vector3 newBrickGroupPosition = new Vector3(victim.BrickGroup.transform.position.x,
		victim.BrickGroup.transform.position.y - (lineSpaceConst * victim.BrickGroup.transform.localScale.y));

		victim.BrickGroup.transform.position = newBrickGroupPosition;

		Vector3 newLinePositionWithinParent = new Vector3(0, victim.recentlyAddedLineY + lineSpaceConst);
		victim.recentlyAddedLineY += lineSpaceConst;

		GameObject newLine = Instantiate(LinePrefab, victim.BrickGroup.transform.position, Quaternion.identity) as GameObject;
		newLine.transform.parent = victim.BrickGroup.transform;
		newLine.transform.localPosition = newLinePositionWithinParent;
		newLine.transform.localScale = Vector3.one;	
	}

    void applyPowerUp(applyPowerUpInfo info)
    {
		Player player;

		if (info.powerUpKey == PowerUpKey.ShrinkPaddle) //Players to act upon for ShrinkPaddle Powerup.
		{
			player = info.player == 1 ? player2 : player1;
		} 
		else//Otherwise, keep them in sync
		{
			player = info.player == 1 ? player1 : player2;
		}

        Power.Apply(info, player);
    }


    //MESSENGER METHOD
    //a ball has been destroyed. Update a Player's Ball Game Object. FOR Multi-ball powerup.
    void ballDestroyed(int playerNumber)
    {
        //Player whose ball has been destroyed.
        Player tempPlayer = playerNumber == 1 ? player1 : player2;

        if (tempPlayer.Ball == null || !tempPlayer.Ball.activeInHierarchy)
        {
            print("renewed in ballDestroyed");
            tempPlayer.Ball = GameObject.FindGameObjectWithTag("Ball" + playerNumber);
        }
    }
}

public class Player
{
    public int playerNumber { get; set; }

    public int wins { get; set; }

    public float score { get; set; }
    public int comboCount { get; set; }

    public int brickCount { get; set; }                     

    public int pendingBricks { get; set; }  // Bricks player will send to victim.

    public GameObject BrickGroup { get; set; }
    public GameObject Paddle { get; set; }
    public GameObject Ball { get; set; }

    internal float recentlyAddedLineY;

	public string name { get; set; }

    public Player()
    { }
}

public class Power : MonoBehaviour
{
	public static void Apply(applyPowerUpInfo power, Player player)
    {
		switch (power.powerUpKey)
        {
            case PowerUpKey.GrowPaddle:
				GrowPaddle(player, power);
                break;
			case PowerUpKey.ShrinkPaddle:
				ShrinkPaddle(player, power);
				break;
            case PowerUpKey.MultiBall:
				MultiBall(player);
                break;
            case PowerUpKey.AddLine:
                AddLine(player , power);
                break;
        }
    }

    static void AddLine(Player player, applyPowerUpInfo power)
    {
        GameManager GMref = (GameManager)GameObject.Find("Game Manager").GetComponent<GameManager>();

        int linesToAdd = UnityEngine.Random.Range(1, power.addLineUpperBound);
        int bricksToAdd = (linesToAdd * GMref.bricksPerLine) - ((int)(GMref.brickValue/GMref.scorePerBrick));

        player.pendingBricks += bricksToAdd;

        GameObject.Find("Game Manager").SendMessage("sendBricks", player.playerNumber);
    }

	static void ResetPaddleToOne(Player player)
	{
		Vector3 newSize = player.Paddle.transform.localScale;
		newSize.x = 1f;

		if (player.Paddle.transform.childCount > 0) 
		{
			if (player.Ball == null)
			{
				print("renewed in MultiBall");
				player.Ball = GameObject.FindGameObjectWithTag("Ball" + player.playerNumber);
			}

			player.Ball.transform.parent = null;
			player.Paddle.transform.localScale = newSize;
			player.Ball.transform.parent = player.Paddle.transform;
		} 
		else 
		{
			player.Paddle.transform.localScale = newSize;
		}
	}
	static void AdjustPaddleSize(Player player, float scaleAmount)
	{
		Vector3 newSize = player.Paddle.transform.localScale;
		newSize.x = newSize.x + scaleAmount;

		if (player.Paddle.transform.childCount > 0) 
		{
			if (player.Ball == null)
			{
				print("renewed in MultiBall");
				player.Ball = GameObject.FindGameObjectWithTag("Ball" + player.playerNumber);
			}

			player.Ball.transform.parent = null;
			player.Paddle.transform.localScale = newSize;
			player.Ball.transform.parent = player.Paddle.transform;
		} 
		else 
		{
			player.Paddle.transform.localScale = newSize;
		}
	}
	static void GrowPaddle(Player player, applyPowerUpInfo power)//Grow paddle by scaleConstant when the player collects the GrowPaddle powerup
	{
		if (player.Paddle.transform.localScale.x == 1f) //Increase size of paddle ONLY if paddle is size 1
		{
			AdjustPaddleSize (player, power.scaleAmount);
		}
		if (player.Paddle.transform.localScale.x < 1f)//reset size of paddle if paddle was shrunken
		{
			ResetPaddleToOne (player);	
		} 
	}
	static void ShrinkPaddle(Player player, applyPowerUpInfo power)//Decrease opponents paddle by scaleConstant when player collects ShrinkPaddle powerup
	{
		if (player.Paddle.transform.localScale.x == 1f) //Decrease size of paddle ONLY if paddle is size 1
		{
			AdjustPaddleSize (player, -power.scaleAmount);
		}
		if (player.Paddle.transform.localScale.x > 1f)//reset size of paddle if paddle was grown
		{
			ResetPaddleToOne (player);
		} 
	}

    static void MultiBall(Player player)
    {
        if (player.Ball == null)
        {
            print("renewed in MultiBall");
            player.Ball = GameObject.FindGameObjectWithTag("Ball" + player.playerNumber);
        }

        GameObject newBall = GameObject.Instantiate(player.Ball, player.Ball.transform.position, Quaternion.identity) as GameObject;
        Ball castedNewBall = newBall.GetComponent<Ball>();

        castedNewBall.Player = player.playerNumber;
        castedNewBall.ballInPlay = true;

        castedNewBall.tag = "Ball" + player.playerNumber;

        castedNewBall.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        castedNewBall.GetComponent<Rigidbody2D>().isKinematic = false;

        castedNewBall.GetComponent<Rigidbody2D>().AddForce(new Vector3(Input.GetAxis("Horizontal" + player.playerNumber) * castedNewBall.StartingSpeed, castedNewBall.StartingSpeed));
    }

}