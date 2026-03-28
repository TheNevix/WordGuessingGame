<script>
  import { username, matchData, matchCountdown, isRematch, activeTag } from '../stores.js';
  import { t } from '../i18n.js';
  import Banner from '../components/Banner.svelte';

  $: countdownText = $matchCountdown > 1
    ? $t('countdown.starts_in', { n: $matchCountdown })
    : $matchCountdown === 1
      ? $t('countdown.starts_in_one', { n: $matchCountdown })
      : $t('countdown.starts_now');
</script>

<div class="countdown-layout">
  <p class="countdown-title">{$isRematch ? $t('countdown.rematch') : $t('countdown.match_found')}</p>

  <div class="countdown-players">
    <Banner
      username={$matchData?.player1}
      pfp={$matchData?.player1Pfp}
      color={$matchData?.player1BannerColor ?? '#5b21b6'}
      tags={$matchData?.player1 === $username ? ($activeTag ? [$activeTag] : []) : ($matchData?.player1ActiveTag ? [$matchData.player1ActiveTag] : [])}
      isYou={$matchData?.player1 === $username}
      size="lg"
    />

    <div class="countdown-vs">VS</div>

    <Banner
      username={$matchData?.player2}
      pfp={$matchData?.player2Pfp}
      color={$matchData?.player2BannerColor ?? '#5b21b6'}
      tags={$matchData?.player2 === $username ? ($activeTag ? [$activeTag] : []) : ($matchData?.player2ActiveTag ? [$matchData.player2ActiveTag] : [])}
      isYou={$matchData?.player2 === $username}
      size="lg"
    />
  </div>

  <p class="countdown-number">{$matchCountdown > 0 ? $matchCountdown : '🎮'}</p>
  <p class="countdown-sub">{countdownText}</p>
</div>
