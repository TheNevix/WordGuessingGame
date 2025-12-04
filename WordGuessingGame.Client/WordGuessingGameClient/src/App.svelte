<script>
  import { onMount } from "svelte";
  import * as signalR from "@microsoft/signalr";

  let text = "";
  let connection;
  let isWaiting = false;
  let gameStarted = false;
  let gameInformation;
  let logMessages = [];
  let letters = []; // Array of characters shown in the UI
  let chatMessage = "";
  let winnerMessage;
  let isWon = false;
  let currentTurn = null;
  let rematchCount = 0;
  let hasVotedRematch = false;

  onMount(() => {
    // Build connection ONCE
    connection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7241/gamehub")
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Add listeners ONCE
    connection.on("GameStarted", (gameInfo) => {
      console.log("Game started!", gameInfo);
      gameStarted = true;
      logMessages = [];
      letters = [];
      chatMessage = "";
      winnerMessage = null;
      isWon = false;
      rematchCount = 0;
      hasVotedRematch = false;
      gameInformation = gameInfo;
      letters = Array(gameInfo.currentWordLength).fill("");
      currentTurn = gameInfo.turn
    });

    connection.on("Guessed", (guessInfo) => {
      console.log("Guess received!", guessInfo);
      logMessages = [...logMessages, guessInfo.message];

      // If guess is correct → update squares
        if (guessInfo.correctGuess) {
            // Fill in the positions
            for (const index of guessInfo.indexes) {
            letters[index] = guessInfo.guess.toUpperCase();
            }

            // Force Svelte to update by creating a new array reference
            letters = [...letters];
        }

        currentTurn = guessInfo.turn

    });

    connection.on("WordGuessed", (wordGuessedInfo) => {
      console.log("Word has been guessed!", wordGuessedInfo);
      winnerMessage = wordGuessedInfo.message;
      isWon = true;
    });

    connection.on("Rematch", () => {
      console.log("Opponent wants rematch");
      rematchCount++;
    });

    connection.on("Disconnected", () => {
      console.log("Opponent has left!");
      isWaiting = true;
      gameStarted = false;
    });
  });

  async function connectToHub() {
    if (!text) {
      alert("Please enter your name first.");
      return;
    }

    try {
      // Start the already configured connection
      await connection.start();
      console.log("Connected to Game Hub!");

      // Tell server our name
      connection.invoke("RegisterName", text);

      isWaiting = true;

    } catch (err) {
      console.error("Connection failed:", err);
      alert("Could not connect to hub.");
    }
  }

  function sendChat() {
    if (!chatMessage.trim()) return;
    connection.invoke("Guess", chatMessage);
    chatMessage = "";
  }

  function sendRematch() {
  connection.invoke("RematchVote");
  hasVotedRematch = true;
}

</script>

<style>
  :global(body) {
    margin: 0;
    background: black;
    font-family: sans-serif;
  }

  .container {
    height: 100vh;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 1.5rem;
  }

  .field-wrapper {
    width: 400px;
    display: flex;
    flex-direction: column;
    gap: 1.2rem;
  }

  input,
  button {
    width: 100%;
    padding: 1rem 1.25rem;
    font-size: 1.4rem;
    border: 3px solid #00ff00;
    border-radius: 6px;
    background: transparent;
    color: #00ff00;
    outline: none;
    box-sizing: border-box;
  }

  input::placeholder {
    color: #00ff00;
    opacity: 1;
  }

  button {
    cursor: pointer;
    text-align: center;
  }

  button:hover {
    background: rgba(0, 255, 0, 0.15);
  }
  .word-container {
    display: flex;
    gap: 1rem;
    margin-top: 2rem;
  }

  .letter-box {
    width: 60px;
    height: 60px;
    border: 3px solid #00ff00;
    color: #00ff00;
    display: flex;
    justify-content: center;
    align-items: center;
    font-size: 2rem;
    text-transform: uppercase;
  }
  .log-box {
  margin-top: 2rem;
  width: 600px;
  height: 200px;
  border: 3px solid #00ff00;
  border-radius: 6px;
  padding: 1rem;
  overflow-y: auto;
  box-sizing: border-box;
  background: rgba(0, 255, 0, 0.05);
}

.log-line {
  color: #00ff00;
  font-size: 1.2rem;
  margin-bottom: 0.4rem;
  word-break: break-word;
  font-family: monospace;
}
.chat-wrapper {
  display: flex;
  gap: 1rem;
  margin-top: 1rem;
}

.chat-wrapper input,
.chat-wrapper button {
  flex: 1;
  padding: 1rem;
  border: 3px solid #00ff00;
  background: transparent;
  color: #00ff00;
  border-radius: 6px;
}
</style>

<div class="container">

  <!-- If NOT waiting and NOT game started -->
  {#if !isWaiting && !gameStarted}
    <div class="field-wrapper">
      <input
        type="text"
        bind:value={text}
        placeholder="Enter your name..."
      />

      <button on:click={connectToHub}>
        Connect to Hub
      </button>
    </div>
  {/if}

  <!-- If waiting -->
  {#if isWaiting && !gameStarted}
    <div style="color: #00ff00; font-size: 1.6rem;">
      Waiting for another player to start the game...
    </div>
  {/if}

  <!-- If game started -->
  {#if gameStarted}

    {#if !isWon}
      <!-- Normal Game UI -->

      <div style="color: #00ff00; font-size: 1.6rem;">
        🎮 Game Started!
      </div>

      {#if gameInformation}
        <div class="word-container">
          {#each letters as letter}
            <div class="letter-box">{letter}</div>
          {/each}
        </div>
      {/if}

      <div class="log-box">
        {#each logMessages as msg}
          <div class="log-line">{msg}</div>
        {/each}
      </div>

      {#if currentTurn === text}
  <!-- It is MY turn -->
  <div class="chat-wrapper">
    <input
      type="text"
      bind:value={chatMessage}
      placeholder="Guess a letter..."
      on:keydown={(e) => e.key === 'Enter' && sendChat()}
    />
    <button on:click={sendChat}>Send</button>
  </div>
{:else}
  <!-- It is opponent's turn -->
  <div style="color: #00ff00; font-size: 1.5rem; margin-top: 1rem;">
    ✋ Waiting for opponent to guess...
  </div>
{/if}

    {:else}
      <!-- WINNER SCREEN -->

      <div style="color: #00ff00; font-size: 2rem; margin-top: 2rem; text-align: center;">
        {winnerMessage}
      </div>
      <div style="color:#00ff00; font-size:1.4rem; margin-top:1rem;">
        {rematchCount}/2 players want a rematch
      </div>

      {#if !hasVotedRematch}
        <button
            on:click={() => sendRematch()}
            style="width: 260px; margin-top: 2rem; font-size: 1.4rem;"
        >
            🔁 Rematch
        </button>
        {:else}
        <div style="color: #00ff00; font-size: 1.2rem; margin-top: 1.5rem;">
            ✔ You voted for a rematch — waiting for opponent...
        </div>
      {/if}

    {/if}

  {/if}

</div>