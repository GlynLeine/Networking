using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using shared;
using TMPro;

public class GameManager : MonoBehaviour
{
    public UnityClient client;
    public GameObject lobby;
    public PopUpManager popUpManager;
    public BoardItem[] board;

    public TMP_Text playerNameA;
    public TMP_Text playerNameB;
    public TMP_Text turnBoard;

    bool readyForTurn;

    ClientData opponent;

    private void Start()
    {
        GameEnd.onGameEnd += OnGameEnd;
        GameStart.onGameStart += StartGame;
        gameObject.SetActive(false);
    }

    public void StartGame(ClientData opponent, bool firstTurn)
    {
        playerNameA.text = client.UserName;
        playerNameB.text = opponent.username;

        Debug.Log("started game");
        this.opponent = opponent;
        readyForTurn = firstTurn;

        if(firstTurn)
            turnBoard.text = "Your turn";
        else
            turnBoard.text = "Opponents turn";

        lobby.gameObject.SetActive(false);
        gameObject.SetActive(true);
        GameMove.onGameMove += RegisterMove;

        foreach(BoardItem boardItem in board)
            boardItem.Reset();
    }

    public void GiveUp()
    {
        client.Write(new Forfeit(client.Id));
    }

    public void DoTurn(int move)
    {
        if (readyForTurn)
        {
            readyForTurn = false;
            client.Write(new GameMove(client.Id, move));

            foreach (BoardItem boardItem in board)
                boardItem.Disable();

            turnBoard.text = "Opponents turn";
        }
    }

    public void RegisterMove(GameMove move)
    {
        Debug.Log(move.MoveId);
        if (move.PlayerId == client.Id)
        {
            board[move.MoveId].DoMove("X");
        }
        else if (move.PlayerId == opponent.id)
        {
            board[move.MoveId].DoMove("O");
            readyForTurn = true;
            turnBoard.text = "Your turn";

            foreach (BoardItem boardItem in board)
                boardItem.Enable();
        }
    }

    public void OnGameEnd(uint winnerId, string reason)
    {
        popUpManager.ShowPopUp(reason);

        lobby.gameObject.SetActive(true);
        gameObject.SetActive(false);
        GameMove.onGameMove -= RegisterMove;
    }
}
