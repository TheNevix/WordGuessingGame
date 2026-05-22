<script>
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';

  onMount(async () => {
    const invite = new URLSearchParams(window.location.search).get('invite');
    if (invite) { goto(`/join?invite=${invite}`); return; }
    const res = await fetch('/api/auth/token');
    const { token } = await res.json();
    goto(token ? '/dashboard' : '/login');
  });
</script>
