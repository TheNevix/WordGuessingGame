<script>
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { t } from '$lib/i18n.js';

  const tabs = [
    { path: '/home',         icon: '⚡', key: 'nav.play' },
    { path: '/uitdagingen',  icon: '✦', key: 'nav.challenges_tab' },
    { path: '/ranglijst',    icon: '▲', key: 'nav.leaderboard_tab' },
    { path: '/profiel',      icon: '◉', key: 'nav.profile_tab' },
  ];
</script>

<nav class="bottom-nav">
  {#each tabs as tab}
    <button
      class="bnav-tab {$page.url.pathname === tab.path ? 'bnav-active' : ''}"
      on:click={() => goto(tab.path)}
    >
      <span class="bnav-icon">{tab.icon}</span>
      <span class="bnav-label">{$t(tab.key)}</span>
    </button>
  {/each}
</nav>
