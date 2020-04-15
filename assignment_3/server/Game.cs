using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using shared;

public class Game
{
    uint[] board;

    public CustomDataClient<ClientData> playerA;
    public CustomDataClient<ClientData> playerB;

    public Action<Game, CustomDataClient<ClientData>> onGameEnd;

    public Game(CustomDataClient<ClientData> playerA, CustomDataClient<ClientData> playerB)
    {
        this.playerA = playerA;
        this.playerB = playerB;

        playerA.onDisconnect += OnDisconnect;
        playerB.onDisconnect += OnDisconnect;

        board = new uint[9];
        GameMove.onGameMove += RegisterMove;
        Forfeit.onForfeit += OnForfeit;

        playerA.Write(new GameStart(playerB.data, true));
        playerB.Write(new GameStart(playerA.data, false));
    }

    void EndGame(CustomDataClient<ClientData> winner)
    {
        playerA.onDisconnect -= OnDisconnect;
        playerB.onDisconnect -= OnDisconnect;

        GameMove.onGameMove -= RegisterMove;
        Forfeit.onForfeit -= OnForfeit;

        onGameEnd?.Invoke(this, winner);
    }

    void OnDisconnect(Client client, string message)
    {
        if (((ClientData)client.data).id == playerA.data.id)
        {
            playerB.Write(new GameEnd(playerB.data.id, "Opponent disconnected."));
            EndGame(playerB);
        }
        else
        {
            playerA.Write(new GameEnd(playerA.data.id, "Opponent disconnected."));
            EndGame(playerA);
        }
    }

    uint GetBoardState(int x, int y)
    {
        int index = x + y * 3;
        if (index < 0 || index >= board.Length)
            return 0;
        return board[index];
    }

    public void Update()
    {
        bool win = false;
        uint winningId = 0;
        #region Horizontal Check
        for (int y = 0; y < 3; y++)
        {
            uint wantedId = GetBoardState(0, y);
            if (wantedId == 0)
                continue;

            win = wantedId == GetBoardState(1, y) && wantedId == GetBoardState(2, y);
            if (win)
            {
                winningId = wantedId;
                break;
            }
        }
        #endregion

        #region Vertical Check
        if (!win)
            for (int x = 0; x < 3; x++)
            {
                uint wantedId = GetBoardState(x, 0);
                if (wantedId == 0)
                    continue;

                win = wantedId == GetBoardState(x, 1) && wantedId == GetBoardState(x, 2);
                if (win)
                {
                    winningId = wantedId;
                    break;
                }
            }
        #endregion

        #region Diagonal Check
        if (!win)
        {
            uint wantedId = GetBoardState(0, 0);
            if (wantedId != 0)
            {
                win = wantedId == GetBoardState(1, 1) && wantedId == GetBoardState(2, 2);
                if (win)
                    winningId = wantedId;
            }

            if (!win)
            {
                wantedId = GetBoardState(2, 0);

                if (wantedId != 0)
                {
                    win = wantedId == GetBoardState(1, 1) && wantedId == GetBoardState(0, 2);
                    if (win)
                        winningId = wantedId;
                }
            }
        }
        #endregion

        if (win)
        {
            if (winningId == playerA.data.id)
            {
                playerA.Write(new GameEnd(playerA.data.id, "You won!"));
                playerB.Write(new GameEnd(playerA.data.id, "You lost..."));

                EndGame(playerA);
            }
            else if (winningId == playerB.data.id)
            {
                playerB.Write(new GameEnd(playerB.data.id, "You won!"));
                playerA.Write(new GameEnd(playerB.data.id, "You lost..."));

                EndGame(playerB);
            }
        }
        else
        {
            bool tied = true;
            foreach (uint index in board)
                if (index == 0)
                {
                    tied = false;
                    break;
                }

            if (tied)
            {
                playerA.Write(new GameEnd(0, "You tied."));
                playerB.Write(new GameEnd(0, "You tied."));

                EndGame(null);
            }
        }
    }

    public void OnForfeit(uint playerId)
    {
        if(playerId == playerA.data.id)
        {
            playerB.Write(new GameEnd(playerB.data.id, "You won!\nOpponent gave up."));
            playerA.Write(new GameEnd(playerB.data.id, "You lost...\nYou gave up."));
            EndGame(playerB);
        }
        else if(playerId == playerB.data.id)
        {
            playerA.Write(new GameEnd(playerA.data.id, "You won!\nOpponent gave up."));
            playerB.Write(new GameEnd(playerA.data.id, "You lost...\nYou gave up."));
            EndGame(playerA);
        }
    }

    public void RegisterMove(GameMove move)
    {
        if (move < 0 || move >= board.Length)
            return;
        if (move.PlayerId == playerA.data.id || move.PlayerId == playerB.data.id)
        {
            if (board[move] != 0)
                return;

            board[move] = move.PlayerId;
            playerA.Write(new GameMove(move.PlayerId, move));
            playerB.Write(new GameMove(move.PlayerId, move));
        }
    }
}

