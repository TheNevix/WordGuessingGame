<script>
  import { username, matchData, matchCountdown, isRematch } from '../stores.js';
  import { t } from '../i18n.js';

  $: countdownText = $matchCountdown > 1
    ? $t('countdown.starts_in', { n: $matchCountdown })
    : $matchCountdown === 1
      ? $t('countdown.starts_in_one', { n: $matchCountdown })
      : $t('countdown.starts_now');
</script>

<div class="countdown-layout">
  <p class="countdown-title">{$isRematch ? $t('countdown.rematch') : $t('countdown.match_found')}</p>

  <div class="countdown-players">
    <div class="countdown-player">
      <div class="countdown-avatar you">
        {#if $matchData?.player1Pfp}
          <img src={$matchData.player1Pfp} alt={$matchData.player1} />
        {:else}
          {$matchData?.player1?.charAt(0).toUpperCase()}
        {/if}
      </div>
      <p class="countdown-name">{$matchData?.player1}</p>
      {#if $matchData?.player1 === $username}<p class="countdown-you-tag">{$t('countdown.you')}</p>{/if}
    </div>

    <div class="countdown-vs">VS</div>

    <div class="countdown-player">
      <div class="countdown-avatar opponent">
        {#if $matchData?.player2Pfp}
          <img src={$matchData.player2Pfp} alt={$matchData.player2} />
        {:else}
          {$matchData?.player2?.charAt(0).toUpperCase()}
        {/if}
      </div>
      <p class="countdown-name">{$matchData?.player2}</p>
      {#if $matchData?.player2 === $username}<p class="countdown-you-tag">{$t('countdown.you')}</p>{/if}
    </div>
  </div>

  <p class="countdown-number">{$matchCountdown > 0 ? $matchCountdown : '🎮'}</p>
  <p class="countdown-sub">{countdownText}</p>
</div>
