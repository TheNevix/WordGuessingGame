<script>
  import { onDestroy } from 'svelte';
  import { page, username, profilePicUrl, bannerColor, userTags, activeTag, isGuest, isWaiting, matchData } from '../stores.js';
  import { connectToHub, cancelWaiting } from '../hub.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  let guestName = "";
  let elapsed = 0;
  let elapsedInterval = null;
  let tipIndex = 0;
  let tipInterval = null;

  const tips = ['lobby.tip_1', 'lobby.tip_2', 'lobby.tip_3', 'lobby.tip_4'];

  // Floating background letters
  const floatingLetters = [
    { l:'W', x:8,  y:15, s:2.5, d:6,  o:0.06 },
    { l:'O', x:88, y:22, s:1.8, d:9,  o:0.05 },
    { l:'R', x:20, y:70, s:3,   d:7,  o:0.07 },
    { l:'D', x:75, y:60, s:2,   d:11, o:0.05 },
    { l:'S', x:50, y:10, s:1.5, d:8,  o:0.04 },
    { l:'A', x:93, y:80, s:2.8, d:5,  o:0.06 },
    { l:'G', x:5,  y:85, s:1.8, d:10, o:0.05 },
    { l:'E', x:60, y:88, s:2.2, d:7,  o:0.06 },
    { l:'U', x:35, y:40, s:1.5, d:13, o:0.04 },
    { l:'S', x:78, y:42, s:2,   d:9,  o:0.05 },
  ];

  $: if ($isWaiting && !$matchData) {
    if (!elapsedInterval) {
      elapsed = 0;
      elapsedInterval = setInterval(() => elapsed++, 1000);
    }
    if (!tipInterval) {
      tipInterval = setInterval(() => tipIndex = (tipIndex + 1) % tips.length, 3000);
    }
  } else {
    if (elapsedInterval) { clearInterval(elapsedInterval); elapsedInterval = null; }
    if (tipInterval)     { clearInterval(tipInterval);     tipInterval = null; }
    if (!$matchData) { elapsed = 0; tipIndex = 0; }
  }

  $: oppName        = $matchData ? ($matchData.player1 === $username ? $matchData.player2 : $matchData.player1) : null;
  $: oppPfp         = $matchData ? ($matchData.player1 === $username ? $matchData.player2Pfp : $matchData.player1Pfp) : null;
  $: oppBannerColor = $matchData ? ($matchData.player1 === $username ? $matchData.player2BannerColor : $matchData.player1BannerColor) ?? '#5b21b6' : '#5b21b6';
  $: oppActiveTag   = $matchData ? ($matchData.player1 === $username ? $matchData.player2ActiveTag : $matchData.player1ActiveTag) : null;
  $: oppTags        = oppActiveTag ? [oppActiveTag] : [];
  $: myName         = $isGuest ? (guestName || 'Guest') : $username;

  function formatElapsed(secs) {
    const m = Math.floor(secs / 60);
    const s = (secs % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
  }

  function doConnect() {
    connectToHub('public', null, $isGuest ? guestName.trim() : $username);
  }

  function goBack() {
    if ($isGuest) { isGuest.set(false); page.set('login'); }
    else          { page.set('dashboard'); }
  }

  onDestroy(() => {
    if (elapsedInterval) clearInterval(elapsedInterval);
    if (tipInterval)     clearInterval(tipInterval);
  });
</script>

<div class="qm-screen">

  <!-- Floating background letters -->
  {#each floatingLetters as fl}
    <span class="qm-float-letter"
      style="left:{fl.x}%; top:{fl.y}%; font-size:{fl.s}rem; animation-duration:{fl.d}s; opacity:{fl.o}"
    >{fl.l}</span>
  {/each}

  <!-- Topbar -->
  <div class="qm-topbar">
    <button class="qm-back-btn" on:click={$isWaiting ? cancelWaiting : goBack}>‹</button>
    <div class="qm-eyebrow">
      <span class="qm-dot"></span>
      {$t('lobby.title')}
    </div>
  </div>

  {#if $matchData}
    <!-- ── Match found ── -->
    <div class="qm-match-found">
      <p class="qm-found-label">{$t('lobby.match_found')}</p>
      <p class="qm-found-sub">{$t('lobby.get_ready')}</p>
      <div class="qm-found-banners">
        <div class="qm-found-banner-wrap">
          <Banner username={myName} pfp={$isGuest ? null : $profilePicUrl}
            color={$isGuest ? '#374151' : $bannerColor}
            tags={$isGuest || !$activeTag ? [] : [$activeTag]} isYou={true} size="md" />
        </div>
        <div class="qm-found-vs">⚡</div>
        <div class="qm-found-banner-wrap qm-found-banner-right">
          <Banner username={oppName} pfp={oppPfp} color={oppBannerColor} tags={oppTags} size="md" />
        </div>
      </div>
    </div>

  {:else if $isWaiting}
    <!-- ── Searching ── -->
    <div class="qm-searching-center">
      <div class="qm-radar-wrap">
        <div class="qm-radar-ring r1"></div>
        <div class="qm-radar-ring r2"></div>
        <div class="qm-radar-ring r3"></div>
        <div class="qm-radar-avatar">
          {#if $profilePicUrl}
            <img src={$profilePicUrl} alt={$username} />
          {:else}
            <span>{$username.charAt(0).toUpperCase()}</span>
          {/if}
        </div>
      </div>
      <p class="qm-searching-label">{$t('lobby.searching_label')}</p>
      <p class="qm-timer">{formatElapsed(elapsed)}</p>
      <p class="qm-tip" key={tipIndex}>{$t(tips[tipIndex])}</p>
      <button class="qm-cancel" on:click={cancelWaiting}>{$t('lobby.cancel')}</button>
    </div>

  {:else}
    <!-- ── Pre-search ── -->
    <div class="qm-presearch">
      <div class="qm-identity">
        <div class="qm-identity-avatar">
          {#if !$isGuest && $profilePicUrl}
            <img src={$profilePicUrl} alt={myName} />
          {:else}
            <span>{myName.charAt(0).toUpperCase()}</span>
          {/if}
          <div class="qm-identity-dot"></div>
        </div>
        <span class="qm-identity-name">{myName}</span>
        {#if !$isGuest && $activeTag}
          <span class="qm-identity-tag">{$activeTag}</span>
        {/if}
      </div>

      {#if $isGuest}
        <div class="qm-name-input">
          <input type="text" bind:value={guestName} placeholder={$t('lobby.guest_placeholder')}
            on:keydown={(e) => e.key === 'Enter' && doConnect()} />
        </div>
      {/if}

      <button class="qm-find-btn" on:click={doConnect}>
        <span class="qm-btn-pulse"></span>
        {$t('lobby.find_match')}
      </button>
      <p class="qm-hint">{$t('lobby.find_hint')}</p>

      <div class="qm-rules">
        <div class="qm-rule"><span>💡</span><span>{$t('lobby.rule_1')}</span></div>
        <div class="qm-rule"><span>🔄</span><span>{$t('lobby.rule_2')}</span></div>
        <div class="qm-rule"><span>🏆</span><span>{$t('lobby.rule_3')}</span></div>
      </div>
    </div>
  {/if}

</div>
