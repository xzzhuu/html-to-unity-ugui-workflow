const list = document.querySelector('.list');
const template = list.querySelector('template');
for (let i = 0; i < 8; i++) {
  const row = template.content.firstElementChild.cloneNode(true);
  row.style.top = `${i * 80}px`;
  row.querySelector('.item-name').textContent = `标准物品 ${i + 1}`;
  row.querySelector('.quantity').textContent = `× ${(i + 1) * 3}`;
  row.onclick = () => { document.querySelector('.detail-title').textContent = `标准物品 ${i + 1}`; document.querySelector('.use').disabled = false; };
  list.append(row);
}
document.querySelector('.search').oninput = e => {
  let index = 0;
  for (const row of list.querySelectorAll('.row')) {
    const visible = row.textContent.includes(e.target.value);
    row.style.display = visible ? '' : 'none';
    if (visible) row.style.top = `${index++ * 80}px`;
  }
};
