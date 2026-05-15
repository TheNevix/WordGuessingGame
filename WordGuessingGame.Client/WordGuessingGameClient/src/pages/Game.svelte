<script>
  import { afterUpdate, onDestroy } from 'svelte';
  import confetti from 'canvas-confetti';
  import {
    username, profilePicUrl, bannerColor, activeTag,
    gameInformation, matchData,
    logMessages, letters, chatMessage,
    winnerMessage, isWon, currentTurn,
    rematchCount, hasVotedRematch,
    isRankedGame, rankedSeriesScore, rankedSeriesOver, rankedRoundWinner,
    guessTimerActive, guessTimerSecs, rankTransition, gameMode
  } from '../stores.js';

  const themes = {
    ranked:  { bg: 'linear-gradient(160deg,#1a0535 0%,#2d0f5e 50%,#0d0820 100%)', accent: '#f59e0b', accentDim: 'rgba(245,158,11,0.15)', dark: true },
    quick:   { bg: 'linear-gradient(160deg,#051628 0%,#0c2a4a 50%,#020d1a 100%)', accent: '#06b6d4', accentDim: 'rgba(6,182,212,0.15)',   dark: true },
    private: { bg: 'linear-gradient(160deg,#0e0a30 0%,#1a1060 50%,#080520 100%)', accent: '#818cf8', accentDim: 'rgba(129,140,248,0.15)', dark: true },
  };
  $: theme = themes[$gameMode] ?? themes.quick;
  import { sendChat, sendRematch, leaveGame } from '../hub.js';
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

  // Ranked: figure out my series score vs opponent series score
  $: mySeriesWins  = $matchData?.player1 === $username ? $rankedSeriesScore.p1 : $rankedSeriesScore.p2;
  $: oppSeriesWins = $matchData?.player1 === $username ? $rankedSeriesScore.p2 : $rankedSeriesScore.p1;

  // Ranked series over
  $: myRPChange  = $rankedSeriesOver
    ? ($matchData?.player1 === $username ? $rankedSeriesOver.player1RPChange : $rankedSeriesOver.player2RPChange)
    : 0;
  $: myNewRP     = $rankedSeriesOver
    ? ($matchData?.player1 === $username ? $rankedSeriesOver.player1NewRP : $rankedSeriesOver.player2NewRP)
    : 0;
  $: iSeriesWon  = $rankedSeriesOver?.winner === $username;

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
    if (guessedLetters.has(key) && !revealedLetters.has(key)) return;
    wordGuess += key;
  }

  function submitWord() {
    const w = wordGuess.trim().toLowerCase();
    if (!w || !isMyTurn) return;
    sendChat(w);
    wordGuess = '';
  }

  function keyState(key) {
    if (revealedLetters.has(key)) return 'correct';
    if (guessedLetters.has(key)) return 'wrong';
    return 'normal';
  }

  $: timerPct = ($guessTimerSecs / 30) * 100;
  $: timerColor = $guessTimerSecs > 15 ? '#7c3aed' : $guessTimerSecs > 8 ? '#f59e0b' : '#ef4444';

  // Rank transition screen: auto-dismiss after 3.5s
  let showingRankTransition = false;
  let rankTransitionTimeout = null;
  $: if ($rankTransition) {
    showingRankTransition = true;
    if (rankTransitionTimeout) clearTimeout(rankTransitionTimeout);
    rankTransitionTimeout = setTimeout(() => {
      showingRankTransition = false;
      rankTransition.set(null);
      fireConfetti();
    }, 3500);
  }

  // Confetti on series win (no rank transition path)
  let confettiFired = false;
  $: if (iSeriesWon && $rankedSeriesOver && !showingRankTransition && !confettiFired && !$rankTransition) {
    confettiFired = true;
    fireConfetti();
  }
  $: if (!$isRankedGame) confettiFired = false;

  function fireConfetti() {
    if (!iSeriesWon) return;
    confetti({ particleCount: 120, spread: 80, origin: { y: 0.6 }, colors: ['#7c3aed','#a855f7','#fbbf24','#34d399'] });
    setTimeout(() => confetti({ particleCount: 60, spread: 120, origin: { y: 0.4 }, angle: 60 }), 300);
    setTimeout(() => confetti({ particleCount: 60, spread: 120, origin: { y: 0.4 }, angle: 120 }), 500);
  }

  onDestroy(() => { if (rankTransitionTimeout) clearTimeout(rankTransitionTimeout); });
</script>

<div class="game-layout" style="background:{theme.bg}; --accent:{theme.accent}; --accent-dim:{theme.accentDim}">

  <nav class="navbar game-navbar" style="border-bottom-color: var(--accent-dim)">
    <span class="navbar-brand" style="color:rgba(255,255,255,0.9)">{$t('nav.brand')}</span>
    <div class="navbar-right">
      {#if $isRankedGame}
        <span class="game-nav-badge ranked-badge" style="color:var(--accent); border-color:var(--accent-dim)">⚔️ {$t('game.ranked_badge')}</span>
      {:else}
        <span class="game-nav-badge" style="color:var(--accent); border-color:var(--accent-dim)">{$t('game.badge')}</span>
      {/if}
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
        {#if $isRankedGame}
          <span class="series-score" style="color:var(--accent)">{mySeriesWins} — {oppSeriesWins}</span>
          <span class="series-label" style="color:rgba(255,255,255,0.4)">{$t('game.series_label')}</span>
        {:else}
          <span class="game-vs-text" style="color:var(--accent)">VS</span>
          <div class="game-progress-track" style="background:rgba(255,255,255,0.1)">
            <div class="game-progress-fill" style="width:{progress}%; background:var(--accent)"></div>
          </div>
          <span class="game-progress-label" style="color:rgba(255,255,255,0.4)">{$t('game.letters', { revealed: revealedCount, total: totalLetters })}</span>
        {/if}
      </div>

      <div class="opponent-col">
        <Banner
          username={oppName ?? '…'}
          pfp={oppPfp}
          color={oppBannerColor}
          tags={oppTags}
          isYou={false}
          isActive={!isMyTurn}
          size="sm"
        />
        {#if !isMyTurn && !$isWon}
          <div class="thinking-dots"><span></span><span></span><span></span></div>
        {/if}
      </div>

    </div>

    <main class="game-main">

      <div class="game-card" style="background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.08)">
        <p class="game-card-label" style="color:rgba(255,255,255,0.5)">{$t('game.guess_word')}</p>
        <div class="word-container">
          {#each $letters as letter}
            <div class="letter-box {letter ? 'letter-revealed' : ''}"
              style="{letter ? `background:var(--accent); color:#fff; border-color:var(--accent)` : 'background:rgba(255,255,255,0.06); border-color:rgba(255,255,255,0.12); color:transparent'}"
            >{letter}</div>
          {/each}
        </div>
      </div>

      <div class="game-card game-input-card" style="background:rgba(255,255,255,0.05); border:1px solid rgba(255,255,255,0.08)">
        {#if isMyTurn}
          <div class="your-turn-header">
            <p class="your-turn-label" style="color:var(--accent)">{$t('game.your_turn')}</p>
            {#if $isRankedGame && $guessTimerActive}
              <div class="guess-timer" style="--timer-color:{timerColor}">
                <svg class="timer-ring" viewBox="0 0 36 36">
                  <circle cx="18" cy="18" r="15.9" fill="none" stroke="#e5e7eb" stroke-width="2.5"/>
                  <circle cx="18" cy="18" r="15.9" fill="none" stroke={timerColor} stroke-width="2.5"
                    stroke-dasharray="{timerPct} 100" stroke-linecap="round"
                    transform="rotate(-90 18 18)"
                  />
                </svg>
                <span class="timer-num">{$guessTimerSecs}</span>
              </div>
            {/if}
          </div>
          <div class="word-guess-row">
            <input
              type="text"
              value={wordGuess}
              placeholder={$t('game.word_placeholder')}
              maxlength="30"
              readonly
              on:focus={(e) => e.target.blur()}
              style="background:rgba(255,255,255,0.08); border-color:rgba(255,255,255,0.12); color:#f1f5f9"
            />
            <button on:click={submitWord} style="background:var(--accent); color:#06030f">{$t('game.guess_btn')}</button>
          </div>
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
                      style={state === 'correct' ? `background:var(--accent); color:#fff; border-color:var(--accent)` : ''}
                    >{key}</button>
                  {/if}
                {/each}
              </div>
            {/each}
          </div>
        {:else}
          <div class="waiting-turn" style="color:rgba(255,255,255,0.5)">
            <span class="waiting-pulse">⏳</span>
            <span>{$t('game.waiting', { name: $currentTurn })}</span>
          </div>
        {/if}
      </div>

    </main>

  {:else if $isRankedGame && !$rankedSeriesOver}

    <!-- Between-round screen -->
    <main class="game-main game-main-center">
      <div class="round-over-card">
        <div class="round-over-emoji">{$rankedRoundWinner === $username ? '🏆' : '💔'}</div>
        <p class="round-over-title">
          {$rankedRoundWinner === $username ? $t('game.round_won') : $t('game.round_lost')}
        </p>
        <p class="round-over-word">{$winnerMessage}</p>
        <div class="round-series-score">
          <span class="rss-num {mySeriesWins > oppSeriesWins ? 'rss-leading' : ''}">{mySeriesWins}</span>
          <span class="rss-sep">—</span>
          <span class="rss-num {oppSeriesWins > mySeriesWins ? 'rss-leading' : ''}">{oppSeriesWins}</span>
        </div>
        <p class="round-series-label">{$t('game.series_label')}</p>
        <div class="next-round-bar">
          <div class="next-round-fill"></div>
        </div>
        <p class="next-round-label">{$t('game.next_round')}</p>
      </div>
    </main>

  {:else}

    <main class="game-main game-main-center">
      {#if $isRankedGame && $rankedSeriesOver && showingRankTransition && $rankTransition}
        <!-- Rank transition screen -->
        <div class="rank-transition-card">
          <p class="rank-transition-label">{$rankTransition.direction === 'up' ? $t('game.rank_up') : $t('game.rank_down')}</p>
          <div class="rank-transition-tiers">
            <div class="rank-tier-old">
              <span class="rank-tier-icon">{$t(`ranked.tier_icon.${$rankTransition.oldTier.toLowerCase()}`)}</span>
              <span class="rank-tier-name">{$t(`ranked.tier.${$rankTransition.oldTier.toLowerCase()}`)}</span>
            </div>
            <span class="rank-transition-arrow">{$rankTransition.direction === 'up' ? '→' : '→'}</span>
            <div class="rank-tier-new {$rankTransition.direction === 'up' ? 'rank-tier-new-up' : 'rank-tier-new-down'}">
              <span class="rank-tier-icon">{$t(`ranked.tier_icon.${$rankTransition.newTier.toLowerCase()}`)}</span>
              <span class="rank-tier-name">{$t(`ranked.tier.${$rankTransition.newTier.toLowerCase()}`)}</span>
            </div>
          </div>
        </div>
      {:else if $isRankedGame && $rankedSeriesOver}
        <!-- Ranked series end screen -->
        <div class="win-card">
          <div class="win-trophy">{iSeriesWon ? '🏆' : '💔'}</div>
          <p class="winner-text">{iSeriesWon ? $t('game.ranked_win') : $t('game.ranked_loss')}</p>
          <div class="ranked-series-final">
            <span class="rsf-score" style="color:var(--accent)">{$rankedSeriesOver.player1SeriesWins} — {$rankedSeriesOver.player2SeriesWins}</span>
            {#if $rankedSeriesOver.wasForfeit}
              <span class="rsf-forfeit">{$t('game.ranked_forfeit')}</span>
            {/if}
          </div>
          <div class="win-divider"></div>
          <div class="rp-change-row">
            <span class="rp-change {myRPChange >= 0 ? 'rp-gain' : 'rp-loss'}">{myRPChange >= 0 ? '+' : ''}{myRPChange} RP</span>
            <span class="rp-new">{$t('game.ranked_new_rp', { rp: myNewRP })}</span>
          </div>
          <button class="leave-game-btn" on:click={leaveGame}>{$t('game.leave_btn')}</button>
        </div>
      {:else}
        <!-- Regular win screen -->
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
          <button class="leave-game-btn" on:click={leaveGame}>{$t('game.leave_btn')}</button>
        </div>
      {/if}
    </main>

  {/if}

</div>
