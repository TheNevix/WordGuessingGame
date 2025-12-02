<script>
  import * as signalR from "@microsoft/signalr";

  let text = "";
  let connection;

  async function connectToHub() {
    if (!text) {
      alert("Please enter your game code or name first.");
      return;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl("https://localhost:7241/gamehub")   // <-- CHANGE THIS
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    try {
      await connection.start();
      console.log("Connected to Game Hub!");

      // Example: send the text to the hub
      connection.invoke("RegisterName", text)
        .catch(err => console.error("JoinGame error:", err));

      alert("Connected!");
    } catch (err) {
      console.error("Connection failed:", err);
      alert("Could not connect to hub.");
    }
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
</style>

<div class="container">
  <div class="field-wrapper">
    <input
      type="text"
      bind:value={text}
      placeholder="Type something..."
    />

    <button on:click={connectToHub}>
      Connect to Hub
    </button>
  </div>
</div>
