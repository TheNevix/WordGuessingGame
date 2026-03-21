<script>
  import { afterUpdate } from 'svelte';
  import {
    username, profilePicUrl, gameInformation, matchData,
    logMessages, letters, chatMessage,
    winnerMessage, isWon, currentTurn,
    rematchCount, hasVotedRematch
  } from '../stores.js';
  import { sendChat, sendRematch } from '../hub.js';
  import { t } from '../i18n.js';

  let logEl;
  afterUpdate(() => {
    if (logEl) logEl.scrollTop = logEl.scrollHeight;
  });

  $: isMyTurn = $currentTurn === $username;

  $: oppName = $gameInformation
    ? ($gameInformation.player1 === $username ? $gameInformation.player2 : $gameInformation.player1)
    : null;

  $: myPfp = $profilePicUrl;
  $: oppPfp = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2Pfp : $matchData.player1Pfp)
    : null;

  $: revealedCount = $letters.filter(l => l !== '').length;
  $: totalLetters  = $letters.length;
  $: progress      = totalLetters > 0 ? (revealedCount / totalLetters) * 100 : 0;
</script>

<div class="game-layout">

  <nav class="navbar">
    <span class="navbar-brand">{$t('nav.brand')}</span>
    <div class="navbar-right">
      <span class="game-nav-badge">{$t('game.badge')}</span>
    </div>
  </nav>

  {#if !$isWon}

    <div class="game-players-banner">

      <div class="game-player {isMyTurn ? 'game-player-active' : ''}">
        <div class="game-player-avatar">
          {#if myPfp}
            <img src={myPfp} alt={$username} />
          {:else}
            {$username.charAt(0).toUpperCase()}
          {/if}
          {#if isMyTurn}<div class="turn-dot"></div>{/if}
        </div>
        <span class="game-player-name">
          {$username}
          <span class="you-tag">{$t('game.you')}</span>
        </span>
      </div>

      <div class="game-vs-col">
        <span class="game-vs-text">VS</span>
        <div class="game-progress-track">
          <div class="game-progress-fill" style="width:{progress}%"></div>
        </div>
        <span class="game-progress-label">{$t('game.letters', { revealed: revealedCount, total: totalLetters })}</span>
      </div>

      <div class="game-player {!isMyTurn ? 'game-player-active' : ''}">
        <div class="game-player-avatar">
          {#if oppPfp}
            <img src={oppPfp} alt={oppName} />
          {:else}
            {oppName ? oppName.charAt(0).toUpperCase() : '?'}
          {/if}
          {#if !isMyTurn}<div class="turn-dot"></div>{/if}
        </div>
        <span class="game-player-name">{oppName ?? '…'}</span>
      </div>

    </div>

    <main class="game-main">

      <div class="game-card">
        <p class="game-card-label">{$t('game.guess_word')}</p>
        <div class="word-container">
          {#each $letters as letter}
            <div class="letter-box {letter ? 'letter-revealed' : ''}">{letter}</div>
          {/each}
        </div>
      </div>

      <div class="game-card">
        <p class="game-card-label">{$t('game.activity')}</p>
        <div class="log-box" bind:this={logEl}>
          {#if $logMessages.length === 0}
            <p class="log-empty">{$t('game.no_guesses')}</p>
          {/if}
          {#each $logMessages as msg}
            <div class="log-line">{msg}</div>
          {/each}
        </div>
      </div>

      <div class="game-card game-input-card">
        {#if isMyTurn}
          <p class="your-turn-label">{$t('game.your_turn')}</p>
          <div class="chat-wrapper">
            <input
              type="text"
              bind:value={$chatMessage}
              placeholder={$t('game.placeholder')}
              maxlength="30"
              on:keydown={(e) => e.key === 'Enter' && sendChat($chatMessage)}
            />
            <button on:click={() => sendChat($chatMessage)}>{$t('game.guess_btn')}</button>
          </div>
        {:else}
          <div class="waiting-turn">
            <span class="waiting-pulse">⏳</span>
            <span>{$t('game.waiting', { name: $currentTurn })}</span>
          </div>
        {/if}
      </div>

    </main>

  {:else}

    <main class="game-main game-main-center">
      <div class="win-card">
        <div class="win-trophy">🏆</div>
        <p class="winner-text">{$winnerMessage}</p>
        <div class="win-divider"></div>
        <p class="rematch-info">{$t('game.rematch_votes', { count: $rematchCount })}</p>
        {#if !$hasVotedRematch}
          <button class="rematch-btn" on:click={sendRematch}>{$t('game.rematch_btn')}</button>
        {:else}
          <p class="voted-text">{$t('game.voted')}</p>
        {/if}
      </div>
    </main>

  {/if}

</div>
