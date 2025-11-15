import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UrlService } from '../../services/url.service';

@Component({
  selector: 'app-url-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './url-list.component.html',
  styleUrl: './url-list.component.scss'
})
export class UrlListComponent implements OnInit {
  urls: any[] = [];
  filteredUrls: any[] = [];
  searchTerm: string = '';
  copiedCode: string = '';

  constructor(private urlService: UrlService) {}

  ngOnInit() {
    this.loadPublicUrls();
  }

  loadPublicUrls() {
    this.urlService.getPublicUrls().subscribe({
      next: (data) => {
        this.urls = data;
        this.filteredUrls = data;
      },
      error: (err) => {
        console.error('Error loading URLs:', err);
      }
    });
  }

  onSearch() {
    if (!this.searchTerm.trim()) {
      this.filteredUrls = this.urls;
      return;
    }

    const term = this.searchTerm.toLowerCase();
    this.filteredUrls = this.urls.filter(url => 
      url.shortURL.toLowerCase().includes(term) ||
      url.originalURL.toLowerCase().includes(term) ||
      url.code.toLowerCase().includes(term)
    );
  }

  copyToClipboard(shortUrl: string, code: string) {
    navigator.clipboard.writeText(shortUrl);
    this.copiedCode = code;
    setTimeout(() => (this.copiedCode = ''), 2000);
  }

  deleteUrl(code: string) {
    if (!confirm('Are you sure you want to delete this URL?')) {
      return;
    }

    this.urlService.deleteUrl(code).subscribe({
      next: () => {
        this.urls = this.urls.filter(url => url.code !== code);
        this.filteredUrls = this.filteredUrls.filter(url => url.code !== code);
      },
      error: (err) => {
        alert('Failed to delete URL');
        console.error('Error deleting URL:', err);
      }
    });
  }
}
