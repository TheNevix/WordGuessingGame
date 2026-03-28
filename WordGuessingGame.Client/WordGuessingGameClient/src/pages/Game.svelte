<script>
  import { afterUpdate } from 'svelte';
  import {
    username, profilePicUrl, bannerColor, activeTag,
    gameInformation, matchData,
    logMessages, letters, chatMessage,
    winnerMessage, isWon, currentTurn,
    rematchCount, hasVotedRematch
  } from '../stores.js';
  import { sendChat, sendRematch } from '../hub.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let logEl;
  afterUpdate(() => {
    if (logEl) logEl.scrollTop = logEl.scrollHeight;
  });

  $: isMyTurn = $currentTurn === $username;

  $: oppName = $gameInformation
    ? ($gameInformation.player1 === $username ? $gameInformation.player2 : $gameInformation.player1)
    : null;

  $: oppPfp = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2Pfp : $matchData.player1Pfp)
    : null;

  $: oppBannerColor = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2BannerColor : $matchData.player1BannerColor) ?? '#5b21b6'
    : '#5b21b6';

  $: oppActiveTag = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2ActiveTag : $matchData.player1ActiveTag)
    : null;
  $: oppTags = oppActiveTag ? [oppActiveTag] : [];

  $: revealedCount = $letters.filter(l => l !== '').length;
  $: totalLetters  = $letters.length;
  $: progress      = totalLetters > 0 ? (revealedCount / totalLetters) * 100 : 0;

  // Keyboard
  const KEYBOARD_ROWS = [
    ['Q','W','E','R','T','Y','U','I','O','P'],
    ['A','S','D','F','G','H','J','K','L'],
    ['Z','X','C','V','B','N','M','⌫']
  ];

  let wordGuess = '';

  $: guessedLetters = new Set(
    $logMessages.flatMap(m => {
      const match = m.match(/'([A-Za-z])'/);
      return match ? [match[1].toUpperCase()] : [];
    })
  );
  $: revealedLetters = new Set($letters.filter(l => l !== '').map(l => l.toUpperCase()));

  function tapKey(key) {
    if (key === '⌫') { wordGuess = wordGuess.slice(0, -1); return; }
    if (!isMyTurn) return;
    // Allow re-tapping revealed (correct) letters, block only wrong guesses
    if (guessedLetters.has(key) && !revealedLetters.has(key)) return;
    wordGuess += key;
  }

  function submitWord() {
    const w = wordGuess.trim().toLowerCase();
    if (!w || !isMyTurn) return;
    console.log('submitWord called, sending:', w);
    sendChat(w);
    wordGuess = '';
  }

  function keyState(key) {
    if (revealedLetters.has(key)) return 'correct';
    if (guessedLetters.has(key)) return 'wrong';
    return 'normal';
  }
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

      <Banner
        username={$username}
        pfp={$profilePicUrl}
        color={$bannerColor}
        tags={$activeTag ? [$activeTag] : []}
        isYou={true}
        isActive={isMyTurn}
        size="sm"
      />

      <div class="game-vs-col">
        <span class="game-vs-text">VS</span>
        <div class="game-progress-track">
          <div class="game-progress-fill" style="width:{progress}%"></div>
        </div>
        <span class="game-progress-label">{$t('game.letters', { revealed: revealedCount, total: totalLetters })}</span>
      </div>

      <Banner
        username={oppName ?? '…'}
        pfp={oppPfp}
        color={oppBannerColor}
        tags={oppTags}
        isYou={false}
        isActive={!isMyTurn}
        size="sm"
      />

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

      <div class="game-card game-input-card">
        {#if isMyTurn}
          <p class="your-turn-label">{$t('game.your_turn')}</p>
          <!-- Word guess input -->
          <div class="word-guess-row">
            <input
              type="text"
              value={wordGuess}
              placeholder={$t('game.word_placeholder')}
              maxlength="30"
              readonly
              on:focus={(e) => e.target.blur()}
            />
            <button on:click={submitWord}>{$t('game.guess_btn')}</button>
          </div>
          <!-- On-screen keyboard -->
          <div class="game-keyboard">
            {#each KEYBOARD_ROWS as row}
              <div class="keyboard-row">
                {#each row as key}
                  {#if key === '⌫'}
                    <button class="key-btn key-backspace" on:click={() => tapKey(key)}>⌫</button>
                  {:else}
                    {@const state = keyState(key)}
                    <button
                      class="key-btn key-{state}"
                      disabled={state === 'wrong'}
                      on:click={() => tapKey(key)}
                    >{key}</button>
                  {/if}
                {/each}
              </div>
            {/each}
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
