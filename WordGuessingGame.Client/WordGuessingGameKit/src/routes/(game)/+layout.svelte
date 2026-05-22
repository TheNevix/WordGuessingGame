<script>
  import { onMount } from 'svelte';
  import { goto } from '$app/navigation';

  onMount(async () => {
    const res = await fetch('/api/auth/token');
    const { token } = await res.json();
    if (!token) goto('/login');
  });
</script>

<slot />
