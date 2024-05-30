# GathererServer

The Gatherer Server component is here to assist with the collection of `SOAPHound.exe` data. To run the Gatherer Server, run:

```
pip3 install -r requirements.txt
flask run --host 0.0.0.0 --port 5000 --cert=adhoc
```

## Notes

- The cache files are in the `cache/` directory
- The dump files are stored in the `build/` directory
