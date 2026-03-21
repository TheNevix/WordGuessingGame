<script>
  import { onDestroy } from 'svelte';
  import { page, username, profilePicUrl, isGuest, isWaiting, matchData } from '../stores.js';
  import { connectToHub, cancelWaiting } from '../hub.js';
  import { t } from '../i18n.js';

  let guestName = "";
  let elapsed   = 0;
  let elapsedInterval = null;

  $: if ($isWaiting && !$matchData) {
    if (!elapsedInterval) {
      elapsed = 0;
      elapsedInterval = setInterval(() => elapsed++, 1000);
    }
  } else {
    if (elapsedInterval) { clearInterval(elapsedInterval); elapsedInterval = null; }
    if (!$matchData) elapsed = 0;
  }

  $: oppName = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2 : $matchData.player1)
    : null;
  $: oppPfp = $matchData
    ? ($matchData.player1 === $username ? $matchData.player2Pfp : $matchData.player1Pfp)
    : null;

  onDestroy(() => { if (elapsedInterval) clearInterval(elapsedInterval); });

  function formatElapsed(secs) {
    const m = Math.floor(secs / 60);
    const s = (secs % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  }

  function doConnect() {
    const name = $isGuest ? guestName.trim() : $username;
    connectToHub('public', null, name);
  }

  function goBack() {
    if ($isGuest) { isGuest.set(false); page.set('login'); }
    else          { page.set('dashboard'); }
  }
</script>

<div class="lobby-layout">

  <div class="lobby-header">
    <button class="lobby-back" on:click={$isWaiting ? cancelWaiting : goBack}>{$t('lobby.back')}</button>
    <span class="lobby-header-title">{$t('lobby.title')}</span>
    <span class="lobby-mode-badge">{$t('lobby.badge')}</span>
  </div>

  <div class="lobby-arena">

    <div class="player-card player-card-you">
      <div class="player-card-avatar">
        {#if $profilePicUrl && !$isGuest}
          <img class="player-card-avatar-img" src={$profilePicUrl} alt={$username} />
        {:else if $isGuest}
          {guestName ? guestName.charAt(0).toUpperCase() : '?'}
        {:else}
          {$username.charAt(0).toUpperCase()}
        {/if}
      </div>
      <p class="player-card-name">{$isGuest ? (guestName || 'Guest') : $username}</p>
      <span class="player-card-tag">{$isGuest ? $t('lobby.guest_tag') : $t('lobby.rookie_tag')}</span>
    </div>

    <div class="vs-divider">
      <div class="vs-ring">VS</div>
      <div class="vs-line"></div>
    </div>

    {#if $matchData}
      <div class="player-card player-card-opponent player-card-found">
        <div class="player-card-avatar">
          {#if oppPfp}
            <img class="player-card-avatar-img" src={oppPfp} alt={oppName} />
          {:else}
            {oppName?.charAt(0).toUpperCase()}
          {/if}
        </div>
        <p class="player-card-name">{oppName}</p>
        <span class="player-card-tag">{$t('lobby.ready_tag')}</span>
      </div>
    {:else}
      <div class="player-card player-card-opponent" class:player-card-searching={$isWaiting}>
        <div class="player-card-avatar avatar-unknown" class:avatar-searching={$isWaiting}>
          {$isWaiting ? '⟳' : '?'}
        </div>
        <p class="player-card-name">{$isWaiting ? $t('lobby.searching') : '???'}</p>
        <span class="player-card-tag">{$isWaiting ? $t('lobby.looking_tag') : $t('lobby.waiting_tag')}</span>
      </div>
    {/if}

  </div>

  {#if $isGuest && !$isWaiting}
    <div class="lobby-name-input">
      <input type="text" bind:value={guestName} placeholder={$t('lobby.guest_placeholder')}
        on:keydown={(e) => e.key === 'Enter' && doConnect()} />
    </div>
  {/if}

  <div class="lobby-cta">
    {#if $matchData}
      <p class="match-found-label">{$t('lobby.match_found')}</p>
      <p class="lobby-hint">{$t('lobby.get_ready')}</p>
    {:else if $isWaiting}
      <div class="searching-status">
        <p class="searching-label">{$t('lobby.searching_label')}</p>
        <p class="searching-timer">{formatElapsed(elapsed)}</p>
      </div>
      <button class="btn-ghost btn-sm" on:click={cancelWaiting}>{$t('lobby.cancel')}</button>
    {:else}
      <button class="find-match-btn" on:click={doConnect}>
        <span class="find-match-pulse"></span>
        {$t('lobby.find_match')}
      </button>
      <p class="lobby-hint">{$t('lobby.find_hint')}</p>
    {/if}
  </div>

  {#if !$isWaiting}
    <div class="lobby-rules">
      <div class="rule-item"><span class="rule-icon">💡</span><span>{$t('lobby.rule_1')}</span></div>
      <div class="rule-item"><span class="rule-icon">🔄</span><span>{$t('lobby.rule_2')}</span></div>
      <div class="rule-item"><span class="rule-icon">🏆</span><span>{$t('lobby.rule_3')}</span></div>
    </div>
  {/if}

</div>
